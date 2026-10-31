namespace AdaraBox.Data.Migrations;

public sealed class Migration003_WorkspaceIconSchema : IMigration
{
    public int Version => 3;
    public string Description => "Add icon column to workspace table for custom logos and emojis";

    public string Sql => @"
        ALTER TABLE workspace ADD COLUMN icon TEXT NOT NULL DEFAULT '📁';
    ";
}
