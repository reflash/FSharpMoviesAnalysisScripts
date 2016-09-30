// Type providers
#r "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages\FSharp.Data.2.3.1/lib/net40/FSharp.Data.dll"
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

// Types
type ImdbMovies = CsvProvider<"movie_metadata.csv", IgnoreErrors=true>
type ImdbMovie = ImdbMovies.Row
type Movie = { Id : string; 
               Title : string; 
               Score : float; 
               Budget : int option;
               Likes : int;
               Director : string }

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
    Likes = m.Cast_total_facebook_likes;
    Director = m.Director_name }

let documentToMovie (m:BsonDocument) =
    { Id = m.GetElement("_id").Value.ToString(); 
      Title = m.GetElement("Title").Value.ToString(); 
      Score = m.GetElement("Score").Value.ToDouble(); 
      Budget = optionOfNullable (m.GetElement("Budget").Value.AsNullableInt32);
      Likes = m.GetElement("Likes").Value.ToInt32();
      Director = m.GetElement("Director").Value.ToString() }

let movieToDocument (m:Movie) = BsonDocument ([ BsonElement("_id" , BsonObjectId(ObjectId(m.Id)) ); 
                                                       BsonElement("Title" , BsonValue.Create m.Title ); 
                                                       BsonElement("Score" , BsonValue.Create m.Score ); 
                                                       BsonElement("Budget", BsonValue.Create (nullableOfOption m.Budget) ); 
                                                       BsonElement("Likes" , BsonValue.Create m.Likes );
                                                       BsonElement("Director" , BsonValue.Create m.Director );])

// Parse file and work with some fields
let movies = ImdbMovies.GetSample();

// Insert data to db
let connectionString = "mongodb://localhost"
let client = new MongoClient(connectionString)
let db = client.GetDatabase("MoviesDb")
let imdbMovies = movies.Rows
                    |> Seq.map imdbMovieToMovie
                    |> Seq.map movieToDocument
                    |> Seq.toList

let collection = db.GetCollection<BsonDocument> "movies"
collection.DeleteMany FilterDefinition.Empty
collection.InsertMany imdbMovies


