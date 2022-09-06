## Generate API as Markdown


Selected **mddocs**

Reasons:
- mddocs: it combines overloaded method in a single markdown file
- mddocs: method parameters are not part of the url path - only part of the #hash  
  DefaultDocumentation: instead could create very long filenames
- mddocs: path of markdown files are stable. The are the concatenation of: namespace/class/[constructor|fields|methods|properties]



## **mddocs**

[ap0llo/mddocs](https://github.com/ap0llo/mddocs)


Install Grynwald.MdDocs .NET tool
```
dotnet tool install --global Grynwald.MdDocs
```

Generate Markdown using config file `api-reference.json`  
See default config file [defaultSettings.json](https://github.com/ap0llo/mddocs/blob/master/src/MdDocs.Common/Configuration/defaultSettings.json)
```
mddocs apireference --configurationFilePath scripts/api-reference.json
```


## **DefaultDocumentation**

[Doraku/DefaultDocumentation](https://github.com/Doraku/DefaultDocumentation)

Install DefaultDocumentation .NET tool
```
dotnet tool install DefaultDocumentation.Console -g
```

Generate Markdown
```
defaultdocumentation -a Friflo.Json.Fliox.Hub.dll -o defaultDoc -s Public --FileNameFactory Name
```
