#load "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\KaggleAnalysisScripts\StepCSVtoMongo.fsx"
#load "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\KaggleAnalysisScripts\StepMongotoDataWithPreparations.fsx"

#r "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages/Accord.3.3.0/lib/net45/Accord.dll"
#r "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages/Accord.Math.3.3.0/lib/net45/Accord.Math.dll"
#r "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages/Accord.Statistics.3.3.0/lib/net45/Accord.Statistics.dll"
#r "C:\Users\daniil.ekzaryan\Documents\Visual Studio 2015\Projects\KaggleAnalysisScripts\packages/Accord.MachineLearning.GPL.3.3.0/lib/net45/Accord.MachineLearning.GPL.dll"

open Accord
open Accord.Statistics.Models.Regression
open Accord.Statistics.Models.Regression.Linear
open Accord.Statistics.Models.Regression.Fitting
open FSharp.Data

// Linear regression

type MovieData = { Predictors : float[][]; Outputs: float[] }
type MovieDataBool = { BoolPredictors : float[][]; BoolOutputs: bool[] }

let getMoviesData1 movies = 
        let inputs = movies |> Seq.map (fun m -> ([| float (Option.get m.Budget); float m.Likes |])) |> Seq.toArray
        let outputs = (movies |> Seq.map (fun m -> m.Score) |> Seq.toArray)
        { Predictors = inputs; Outputs = outputs }

let getMultipleRegression data =
    let regression = new OrdinaryLeastSquares()
    regression.Learn(data.Predictors, data.Outputs)

let computeByMultipleLinearRegression (x:float[]) (r:MultipleLinearRegression) =
    r.Transform(x)

let computeByMultipleLinearRegression2 (r:MultipleLinearRegression) (x:float[]) =
    r.Transform(x)

moviesWithBudget
 |> getMoviesData1
 |> getMultipleRegression 
 |> computeByMultipleLinearRegression [| 225000000.0; 12572.0 |]


let getMoviesData2 score movies = 
        let inputs = movies |> Seq.map (fun m -> ([| float (Option.get m.Budget); float m.Likes |])) |> Seq.toArray
        let outputs = (movies |> Seq.map (fun m -> m.Score > score) |> Seq.toArray)
        { BoolPredictors = inputs; BoolOutputs = outputs }

let getLogisticRegression data =
    let learner = new IterativeReweightedLeastSquares<LogisticRegression>()
    learner.Tolerance <- 1e-4
    learner.Iterations  <- 100
    learner.Regularization <- 0.

    learner.Learn(data.BoolPredictors, data.BoolOutputs)

let computeProbabilityByLogisticRegression (x:float[]) (r:LogisticRegression) =
    r.Probability(x)


moviesWithBudget
 |> getMoviesData2 6.5
 |> getLogisticRegression 
 |> computeProbabilityByLogisticRegression [| 237000000.0; 48350.0 |]

let split70_30 data = 
    let n70 = (Seq.length data)*70/100
    (Seq.take n70 data, Seq.skip n70 data)

let trainSet, validationSet = moviesWithBudget |> split70_30

let trainedModel = trainSet |> getMoviesData1  |> getMultipleRegression
let validate trainedModel = 
    Seq.map (fun m -> (m.Score, computeByMultipleLinearRegression2 trainedModel [| float (Option.get m.Budget); float m.Likes |], float (Option.get m.Budget)))

let trace data=
    let xs = data |> Seq.map (fun (b, _) -> b)
    let y1s = data |> Seq.map (fun (b, s) -> s |> Seq.averageBy (fun (s1:float,_,_) -> s1))
    let y2s = data |> Seq.map (fun (b, s) -> s |> Seq.averageBy (fun (_,s2:float,_) -> s2))
    let real = Scatter(
                    name = "Real data",
                    x = xs,
                    y = y1s
                )
    let calc = Scatter(
                    name = "Predicted data",
                    x = xs,
                    y = y2s
                )
    [real;calc]

validationSet 
 |> validate trainedModel
 |> Seq.groupBy (fun (_,_,b) -> b)
 |> Seq.sortBy (fun (b, _) -> b)
 |> trace
 |> Plotly.Plot
 |> Plotly.WithWidth 700
 |> Plotly.WithHeight 500