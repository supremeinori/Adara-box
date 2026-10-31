namespace AdaraBox.Data.Migrations;

public sealed class Migration002_MediaLinksSchema : IMigration
{
    public int Version => 2;
    public string Description => "Add media_link table for Quick Links (M08 Tahap A)";

    public string Sql => @"
        CREATE TABLE IF NOT EXISTS media_link (
          id          TEXT PRIMARY KEY,
          name        TEXT NOT NULL,
          url         TEXT NOT NULL,
          sort_order  INTEGER NOT NULL DEFAULT 0,
          created_at  TEXT NOT NULL,
          updated_at  TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS ix_media_link_sort_order ON media_link(sort_order);
    ";
}
