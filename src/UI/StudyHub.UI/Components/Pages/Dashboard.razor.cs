using Microsoft.AspNetCore.Components;

namespace StudyHub.UI.Components.Pages;

public partial class Dashboard : ComponentBase
{
    private readonly (string Text, string Time)[] _activity =
    [
        ("Neues Projekt \"Datenbank-Refactoring\" angelegt", "vor 5 Minuten"),
        ("Team-Mitglied \"Alex\" hinzugefügt", "vor 2 Stunden"),
        ("Dokument \"Architektur-Übersicht.pdf\" hochgeladen", "gestern, 18:42 Uhr")
    ];
}
