Imports System.Text
Imports System.Threading

Imports Microsoft.AspNetCore.Http

Imports Hox.Core
Imports Hox.Rendering


Module AppResults

    Public Class HoxStreamedResult
        Implements IResult

        Private ReadOnly _node As Node

        Public Sub New(node As Node)
            _node = node
        End Sub

        Public Async Function ExecuteAsync(context As HttpContext) As Task Implements IResult.ExecuteAsync
            context.Response.ContentType = "text/html; charset=utf-8"
            Dim Reader = context.Response.BodyWriter
            Dim Token = context.RequestAborted
            Dim Node = Chunked.render(_node, Token)
            Await context.Response.StartAsync(Token)

            Dim ChunkEnumerator = Node.GetAsyncEnumerator(Token)

            While Await ChunkEnumerator.MoveNextAsync()
                Dim _Chunk = ChunkEnumerator.Current
                Await Reader.WriteAsync(New ReadOnlyMemory(Of Byte)(System.Text.Encoding.UTF8.GetBytes(_Chunk)), Token)
                Await context.Response.Body.FlushAsync(Token)
            End While

            Await ChunkEnumerator.DisposeAsync()

            Await context.Response.CompleteAsync()
        End Function
    End Class

    Public Function ToStreamedResult(Node As Node) As IResult
        Return New HoxStreamedResult(Node)
    End Function

    Public Async Function ToTextResult(
        Node As Node,
        Optional CancellationToken As CancellationToken = Nothing
    ) As Task(Of IResult)
        Dim Token As CancellationToken
        If CancellationToken = Nothing Then Token = CancellationToken.None Else Token = CancellationToken

        Dim Text = Await Builder.ValueTask.render(Node, Token)

        Return Results.Text(Text, "text/html", Encoding.UTF8)

    End Function

End Module
