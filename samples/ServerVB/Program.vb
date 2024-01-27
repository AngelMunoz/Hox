Imports FSharp.Control
Imports Hox.Core
Imports Hox.NodeDsl
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Http
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.FSharp.Core


Module Layout

    Public Function Base(ByVal Head As Node, ByVal Scripts As Node, ByVal ParamArray Children() As Node) As Node
        Dim HeadValue As Node
        Dim ScriptsValue As Node

        If Head Is Nothing Then
            HeadValue = fragment(Array.Empty(Of Node))
        Else
            HeadValue = Head
        End If

        If Scripts Is Nothing Then
            ScriptsValue = fragment(Array.Empty(Of Node))
        Else
            ScriptsValue = Scripts
        End If

        Return h("html[lang=en].sl-theme-light",
          h("head",
            h("meta[charset=utf-8]"),
            h("meta[name=viewport]
                   [content=width=device-width, initial-scale=1.0]
            "),
            h("title", text("Htmelo")),
            h("link[rel=stylesheet]
                   [href=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/light.css]
                   [media=(prefers-color-scheme:light)]"),
            h("link[rel=stylesheet]
                   [href=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/dark.css]
                   [media=(prefers-color-scheme:dark)]
                   [onload=document.documentElement.classList.add('sl-theme-dark');]"),
          HeadValue
        ),
        h("body",
          fragment(Children),
          h("script[type=module]
                   [src=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/shoelace-autoloader.js]")
          ),
        ScriptsValue
      )
    End Function

    Public Function Base(Children As Node())
        Return Base(Nothing, Nothing, Children)
    End Function

End Module

Module ListItems

    Public Iterator Function GetNumbers() As IEnumerable(Of Node)
        For i = 1 To 10
            Yield h("li", text(i.ToString()))
        Next
    End Function

    Public Async Function GetNumbersAsync() As Task(Of Node)
        Dim Numbers = New List(Of Node)
        For i = 1 To 10
            Await Threading.Tasks.Task.Delay(i)
            Numbers.Add(h("li", text(i.ToString())))
        Next
        Return fragment(Numbers)
    End Function

    Public Function Thing() As IAsyncEnumerable(Of Node)
        Dim values = New List(Of Integer)({1, 2, 3, 4, 5, 6, 7, 8, 9, 10})

        Dim mapper As Func(Of Integer, Task(Of Node)) =
            Async Function(i)
                Await Threading.Tasks.Task.Delay(i)
                Return h("li", text(i.ToString()))
            End Function
        Return TaskSeq.mapAsync(Of Integer, Task(Of Node), Node)(FuncConvert.FromFunc(mapper), TaskSeq.ofSeq(values))
    End Function

End Module


Module Program
    Sub Main(args As String())

        Dim Builder = WebApplication.CreateBuilder(args)
        Builder.Services.AddLogging()
        Dim app = Builder.Build()

        app.MapGet(
            "/",
            Function(ctx As HttpContext) As IResult
                Dim Content =
                    Base({
                        h("h1", text("Hello World!")),
                        h("p", text("This is a paragraph.")),
                        h("ul[type=sync]", GetNumbers()),
                        h("ul[type=async]", Thing()),
                        h("ul[type=asyncenum]", h(GetNumbersAsync()))
                    })
                Return AppResults.ToStreamedResult(Content)
            End Function
        )

        app.MapGet(
            "/string",
            Function(ctx As HttpContext) As IResult
                Dim Content =
                    Base({
                        h("h1", text("Hello World!")),
                        h("p", text("This is a paragraph.")),
                        h("ul[type=sync]", GetNumbers()),
                        h("ul[type=async]", Thing()),
                        h("ul[type=asyncenum]", h(GetNumbersAsync()))
                    })
                Return AppResults.ToTextResult(Content)
            End Function)

        app.Run()
    End Sub
End Module
