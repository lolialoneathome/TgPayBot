public class Configuration
{
    public string bot_api_key { get; set; }
    public string db_path { get; set; }
    public string userdatafile { get; set; }
    public Spreadsheet[] spreadsheets { get; set; }
}

public class Spreadsheet
{
    public string Id { get; set; }
    public SheetList[] Lists { get; set; }
}

public class SheetList
{
    public string listname { get; set; }
    public string start_column { get; set; }
    public string end_column { get; set; }
    public string isSendedColumn { get; set; }
}
