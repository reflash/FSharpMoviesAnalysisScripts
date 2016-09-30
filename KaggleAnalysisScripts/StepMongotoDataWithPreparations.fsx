//2 лаба - сделать на любом языке для этого сета анализатор, 
//а ля такого https://habrahabr.ru/company/mlclass/blog/270973/ 
//который бы выводил графики и/или тренды по какой либо закономерности в данных

#load "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\KaggleAnalysisScripts\StepCSVtoMongo.fsx"

#load "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages/FsLab.1.0.1/Themes/DefaultWhite.fsx"
#load "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages/FsLab.1.0.1/FsLab.fsx"

open System
open System.IO
open XPlot.Plotly

let rand = new Random()

let randomColor () =
    let r = rand.Next(255)
    let g = rand.Next(255)
    let b = rand.Next(255)
    sprintf "rgb(%d,%d,%d)" r g b

type Director = string
type DataXY = Data of Director * Movie list
type DisplayData = { director : Director; movies : Movie list}

let createDisplayData (name, movieList) =
    { director = name; movies = movieList}

let createTrace { director = name; movies = movieList} =
    Scatter(
        x = (Seq.toList <| Seq.map (fun m -> Option.get m.Budget) movieList),
        y = (Seq.toList <| Seq.map (fun m -> m.Score) movieList),
        mode = "markers",
        name = name,
        text = (Seq.toList <| Seq.map (fun m -> m.Title) movieList),
        marker =
            Marker(
                color = randomColor (),
                size = 12.,
                line =
                    Line(
                        color = "white",
                        width = 0.5
                    )
            )
    )


let styledLayout =
    Layout(
        title = "Director score by budget",
        xaxis =
            Xaxis(
                title = "Budget",
                showgrid = false,
                zeroline = false
            ),
        yaxis =
            Yaxis(
                title = "Score",
                showline = false
            )
    )

let filter = FilterDefinition<BsonDocument>.Empty
let movies = collection.Find(filter).ToListAsync()
                            |> Async.AwaitTask
                            |> Async.RunSynchronously
                            |> Seq.map documentToMovie


let moviesWithBudget = movies |> Seq.filter (fun m -> Option.isSome m.Budget )

let moviesByDirectorWithBudget = movies
                                    |> Seq.filter (fun m -> Option.isSome m.Budget )
                                    |> Seq.groupBy (fun m -> m.Director)
                                    |> Seq.map (fun (d, ms) -> (d, Seq.toList ms))


