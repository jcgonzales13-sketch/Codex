IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_{{STATE_TABLE}}_UpdatedAt'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[{{STATE_TABLE}}]')
)
BEGIN
    CREATE INDEX [IX_{{STATE_TABLE}}_UpdatedAt]
        ON [{{SCHEMA_NAME}}].[{{STATE_TABLE}}] ([UpdatedAt] DESC);
END;
