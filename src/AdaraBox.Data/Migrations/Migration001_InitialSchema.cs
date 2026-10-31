namespace AdaraBox.Data.Migrations;

public interface IMigration
{
    int Version { get; }
    string Description { get; }
    string Sql { get; }
}

public sealed class Migration001_InitialSchema : IMigration
{
    public int Version => 1;
    public string Description => "Initial schema: workspace, resource (cascade on delete), and settings";

    public string Sql => @"
        CREATE TABLE IF NOT EXISTS workspace (
          id          TEXT PRIMARY KEY,
          name        TEXT NOT NULL,
          created_at  TEXT NOT NULL,
          updated_at  TEXT NOT NULL,
          sort_order  INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS resource (
          id            TEXT PRIMARY KEY,
          workspace_id  TEXT NOT NULL REFERENCES workspace(id) ON DELETE CASCADE,
          name          TEXT NOT NULL,
          type          TEXT NOT NULL CHECK (type IN ('application','file','folder','link')),
          target        TEXT NOT NULL,
          sort_order    INTEGER NOT NULL DEFAULT 0,
          created_at    TEXT NOT NULL,
          updated_at    TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS ix_resource_workspace ON resource(workspace_id, sort_order);

        CREATE TABLE IF NOT EXISTS settings (
          key   TEXT PRIMARY KEY,
          value TEXT NOT NULL
        );
    ";
}
