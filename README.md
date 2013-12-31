# SharpStache
by Kyle Sletten

A [mustache](http://mustache.github.io) renderer for .NET built with performance and style in mind.

## Usage

    var tmp = /* some template string */;
    var obj = /* some object to render */;
    SharpStache.Render(tmp, obj);


## Templates

This is not yet a full implementation of the mustache templating system, but the following things are supported:

### Attributes

A with a given context `person`, a tag `{{Name}}` will render the contents of `ctx.Name.ToString()`. If `person` is `null`, doesn't have an property `Name`, or `person.Name` is null, an empty string will be used instead.

    SharpStache.Render("Hello, {{Name}}", new { Name = "Joe" })
    
will result in:

   Hello, Joe

### Conditionals

With a given context `family`, a tag `{{# Members}}` will render the block until a `{{/ Members}}` tag for each member of the given family.

    SharpStache.Render(
        "Hello{{#Members}}, {{Name}} {{/Members}}",
        new
        {
            Members = new []
            {
                new { Name = "Joe" },
                new { Name = "Jane" }
            }
        })

will result in:

    Hello, Joe, Jane

A tag `{{^ Members}}` will render the block until a `{{/ Members}}` tag only if the family has no members.

    SharpStache.Render(
        "Hello{{#Members}}, {{Name}} {{/Members}}{{# Members}}, World{{/ Members}}",
        null)

will result in:

    Hello, World
