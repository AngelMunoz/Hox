### HTML DSL

```fsharp
open Hox
open Hox.Rendering

let getitems(service, secondService) = taskSeq {
    let! items = service.GetItemsAsync()
    for item in items do
        let! item = secondService.withExtendedInfo item
        h("tr",
            h("td", item.Id),
            h("td", item.Name),
            h("td", item.Description),
            h("td", item.Price),
            h("td", item.Quantity),
            h("td", item.Total)
        )
}

let mainView(container) =
    h("table",
        h("thead",
            h("tr",
                h("th", "Id"),
                h("th", "Name"),
                h("th", "Description"),
                h("th", "Price"),
                h("th", "Quantity"),
                h("th", "Total")
            )
        ),
        h("tbody", getitems(container.GetService(), container.GetService()))
    )

// In an aspnet core handler
let view ctx = task {
    let view = Layout.Default("Hox", mainView(ctx.RequestServices))
    do! Render.toStream(view, ctx.Response.Body, ctx.RequestAborted)
}
```
