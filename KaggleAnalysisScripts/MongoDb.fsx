// Type providers
#r "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages\FSharp.Data.2.3.2/lib/net40/FSharp.Data.dll"
open FSharp.Data

// Charting
#load "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages\FSharp.Charting.0.90.14/FSharp.Charting.fsx"
open System
open FSharp.Charting

// Mongodb
#r "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages\MongoDB.Driver.2.2.4/lib/net45/MongoDB.Driver.dll"
#r "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages\MongoDB.Driver.Core.2.2.4/lib/net45/MongoDB.Driver.Core.dll"
#r "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages\MongoDB.Bson.2.2.4/lib/net45/MongoDB.Bson.dll"

open MongoDB.Bson
open MongoDB.Driver

// крч, есть сайты https://www.kaggle.com и https://www.quandl.com, 
// на них хранятся всякие разные датасеты. 
// 1 лаба - скачать оттуда какой-нибудь датасет на 5 и более столбцов, 
// в формате csv и импортировать в любую nosql базу

// Types
type ImdbMovies = CsvProvider<"movie_metadata.csv", IgnoreErrors=true>
type ImdbMovie = ImdbMovies.Row
type Movie = { Id : string; 
               Title : string; 
               Score : float; 
               Budget : int option;
               Likes : int }

// Converters
let optionOfNullable (a : System.Nullable<'T>) = 
    if a.HasValue then
        Some a.Value
    else
        None

let nullableOfOption = function
    | None -> new System.Nullable<_>()
    | Some x -> new System.Nullable<_>(x)

let imdbMovieToMovie (m:ImdbMovie) =
    { Id = ObjectId.GenerateNewId().ToString(); 
    Title = m.Movie_title; 
    Score = float m.Imdb_score;
    Budget = optionOfNullable m.Budget;
    Likes = m.Cast_total_facebook_likes }

let documentToMovie (m:BsonDocument) =
    { Id = m.GetElement("_id").Value.ToString(); 
      Title = m.GetElement("Title").Value.ToString(); 
      Score = m.GetElement("Score").Value.ToDouble(); 
      Budget = optionOfNullable (m.GetElement("Budget").Value.AsNullableInt32);
      Likes = m.GetElement("Likes").Value.ToInt32() }

let movieToDocument (m:Movie) = BsonDocument ([ BsonElement("_id" , BsonObjectId(ObjectId(m.Id)) ); 
                                                       BsonElement("Title" , BsonValue.Create m.Title ); 
                                                       BsonElement("Score" , BsonValue.Create m.Score ); 
                                                       BsonElement("Budget", BsonValue.Create (nullableOfOption m.Budget) ); 
                                                       BsonElement("Likes" , BsonValue.Create m.Likes );])

// Parse file and work with some fields
let movies = ImdbMovies.GetSample();

for movie in movies.Rows do
    printfn "%A" movie.Country

let someRows = movies.Rows |> Seq.take 100

someRows
 |> Seq.groupBy (fun m -> m.Country)
 |> Seq.map (fun (c, v) -> (c, v |> Seq.sumBy (fun m -> m.Imdb_score)))
 |> Chart.Column

// Insert data to db
let connectionString = "mongodb://localhost"
let client = new MongoClient(connectionString)
let db = client.GetDatabase("MoviesDb")
let imdbMovies = movies.Rows
                    |> Seq.map imdbMovieToMovie
                    |> Seq.map movieToDocument
                    |> Seq.toList

let insertCollection = db.GetCollection<BsonDocument> "movies"
insertCollection.InsertMany imdbMovies


// Get data from db
let getCollection = db.GetCollection<BsonDocument> "movies"
let filter = FilterDefinition<BsonDocument>.Empty
getCollection.Find(filter).ToListAsync()
  |> Async.AwaitTask
  |> Async.RunSynchronously
  |> Seq.map documentToMovie
  |> Seq.sortByDescending (fun m -> m.Score)
  |> Seq.take 5
  |> Seq.map (fun m -> m.Title, m.Score)
  |> Chart.Column

