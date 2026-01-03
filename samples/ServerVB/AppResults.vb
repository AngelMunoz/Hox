Imports System.Text
Imports System.Threading

Imports Microsoft.AspNetCore.Http

Imports Hox.Core
Imports Hox.Rendering

Module AppResults

    Public NotInheritable Class HoxStreamedResult
        Implements IResult

        Private ReadOnly _node As Node

        Public Sub New(node As Node)
            _node = node
        End Sub

        Public Async Function ExecuteAsync(context As HttpContext) As Task Implements IResult.ExecuteAsync
            context.Response.ContentType = "text/html; charset=utf-8"
            Dim Reader = context.Response.BodyWriter
            Dim Token = context.RequestAborted
            Await context.Response.StartAsync(Token)

            Await Render.ToStream(_node, context.Response.Body, cancellationToken:=Token)

            Await context.Response.CompleteAsync()
        End Function
    End Class

    Public Function ToStreamedResult(Node As Node) As IResult
        Return New HoxStreamedResult(Node)
    End Function

    Public Class HoxStringResult
        Implements IResult

        Private ReadOnly _node As Node

        Public Sub New(node As Node)
            _node = node
        End Sub

        Public Async Function ExecuteAsync(context As HttpContext) As Task Implements IResult.ExecuteAsync
            context.Response.ContentType = "text/html; charset=utf-8"
            Await context.Response.StartAsync(context.RequestAborted)
            Dim Text = Await Render.AsString(_node, context.RequestAborted)
            Await context.Response.WriteAsync(Text, context.RequestAborted)
            Await context.Response.CompleteAsync()
        End Function
    End Class

    Public Function ToTextResult(Node As Node) As IResult
        Return New HoxStringResult(Node)
    End Function

End Module
