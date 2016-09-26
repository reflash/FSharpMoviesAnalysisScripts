//2 лаба - сделать на любом языке для этого сета анализатор, 
//а ля такого https://habrahabr.ru/company/mlclass/blog/270973/ 
//который бы выводил графики и/или тренды по какой либо закономерности в данных

#load "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\KaggleAnalysisScripts\PrepareAnalysis.fsx"

#load "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages/FsLab.1.0.1/Themes/DefaultWhite.fsx"
#load "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages/FsLab.1.0.1/FsLab.fsx"

open System
open System.IO
open XPlot.Plotly

//* 3D *//
let values f xRange yRange xStep yStep = 
    let xmin, xmax = xRange
    let ymin, ymax = yRange
    let stepByX = xStep
    let stepByY = yStep

    [for x in xmin..stepByX..xmax -> [for y in ymin..stepByY..ymax -> f x y] ]

let valuesWithDefaultSteps f xRange yRange =
    values f xRange yRange 0.1 0.1

let valuesWithDefaultStepsAndRanges f =
    valuesWithDefaultSteps f (-10.,10.) (-10.,10.)

let display layout zValues =
   [Surface(z = zValues)]
        |> Plotly.Plot
        |> Plotly.WithLayout layout

let displayWithDefaultLayout zValues =
    display (Layout(title = "None", autosize = true)) zValues

let html zValues =
    let chart = displayWithDefaultLayout zValues
    chart.GetInlineHtml ()


valuesWithDefaultStepsAndRanges (fun x y -> ( x ** 2. + y ** 2. ))
    |> displayWithDefaultLayout


//* 3D END *//
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
collection.Find(filter).ToListAsync()
  |> Async.AwaitTask
  |> Async.RunSynchronously
  |> Seq.map documentToMovie
  |> Seq.filter (fun m -> Option.isSome m.Budget )
  |> Seq.groupBy (fun m -> m.Director)
  |> Seq.map (fun (d, ms) -> (d, Seq.toList ms))
  |> Seq.map createDisplayData
  |> Seq.map createTrace
  |> Seq.take 7
  |> Plotly.Plot
  |> Plotly.WithLayout styledLayout
  |> Plotly.WithWidth 700
  |> Plotly.WithHeight 500

