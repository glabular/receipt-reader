# Notes

## Publish (single-file)

Run from the **repository root**:

```powershell
dotnet publish ReceiptReader\ReceiptReader.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true
```

Output folder:

`ReceiptReader\bin\Release\net10.0-windows\win-x64\publish\`

### Why these flags

- `PublishSingleFile=true` — packs managed assemblies into one `.exe`.
- `IncludeNativeLibrariesForSelfExtract=true` — embeds native DLLs (`OpenCvSharpExtern.dll`, `Microsoft.Data.SqlClient.SNI.dll`, OpenCV ffmpeg, etc.) into the exe; they extract to a temp folder at runtime instead of sitting next to the exe.
- `IncludeAllContentForSelfExtract=true` — same idea for extra publish content where possible.
- Without the `Include*ForSelfExtract` flags you get those `.dll` / `selenium-manager.exe` files beside the exe.
