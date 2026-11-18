# LogInventory

Ett program som tittar i lösningar byggda i C# för att inventera vad de loggar för något med ILogger, i syfte att inventera vad ett system kan skriva i sina loggar.

Programmet producerar en CSV-fil med vilken fil som loggar vilket meddelande i en lösning.

CSV-filen innehåller följande kolumner:

*Sökväg till .cs-fil; Filnamn; Nivå; Senast ändrad; Loggat meddelande*
