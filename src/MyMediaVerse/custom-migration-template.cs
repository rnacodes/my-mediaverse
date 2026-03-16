// Custom Migration Template - Replace the generated Up() method with this content
// TODO: update if still needed or delete if no longer needed
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Step 1: Create the new Topics and Genres tables
    migrationBuilder.CreateTable(
        name: "Topics",
        columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_Topics", x => x.Id);
        });

    migrationBuilder.CreateTable(
        name: "Genres",
        columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_Genres", x => x.Id);
        });

    // Step 2: Create junction tables
    migrationBuilder.CreateTable(
        name: "MediaItemTopics",
        columns: table => new
        {
            MediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
            TopicId = table.Column<Guid>(type: "uuid", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_MediaItemTopics", x => new { x.MediaItemId, x.TopicId });
            table.ForeignKey(
                name: "FK_MediaItemTopics_MediaItems_MediaItemId",
                column: x => x.MediaItemId,
                principalTable: "MediaItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                name: "FK_MediaItemTopics_Topics_TopicId",
                column: x => x.TopicId,
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        });

    migrationBuilder.CreateTable(
        name: "MediaItemGenres",
        columns: table => new
        {
            MediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
            GenreId = table.Column<Guid>(type: "uuid", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_MediaItemGenres", x => new { x.MediaItemId, x.GenreId });
            table.ForeignKey(
                name: "FK_MediaItemGenres_Genres_GenreId",
                column: x => x.GenreId,
                principalTable: "Genres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                name: "FK_MediaItemGenres_MediaItems_MediaItemId",
                column: x => x.MediaItemId,
                principalTable: "MediaItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        });

    // Step 3: Create unique indexes
    migrationBuilder.CreateIndex(
        name: "IX_Genres_Name",
        table: "Genres",
        column: "Name",
        unique: true);

    migrationBuilder.CreateIndex(
        name: "IX_MediaItemGenres_GenreId",
        table: "MediaItemGenres",
        column: "GenreId");

    migrationBuilder.CreateIndex(
        name: "IX_MediaItemTopics_TopicId",
        table: "MediaItemTopics",
        column: "TopicId");

    migrationBuilder.CreateIndex(
        name: "IX_Topics_Name",
        table: "Topics",
        column: "Name",
        unique: true);

    // Step 4: Migrate existing JSONB data using raw SQL
    migrationBuilder.Sql(@"
        -- Insert unique topics from existing JSONB data
        INSERT INTO ""Topics"" (""Id"", ""Name"")
        SELECT DISTINCT 
            gen_random_uuid() as ""Id"",
            topic_name as ""Name""
        FROM (
            SELECT DISTINCT jsonb_array_elements_text(""Topics"") as topic_name
            FROM ""MediaItems""
            WHERE ""Topics"" IS NOT NULL 
            AND jsonb_array_length(""Topics"") > 0
        ) t
        WHERE topic_name != ''
        ON CONFLICT DO NOTHING;
    ");

    migrationBuilder.Sql(@"
        -- Insert unique genres from existing JSONB data
        INSERT INTO ""Genres"" (""Id"", ""Name"")
        SELECT DISTINCT 
            gen_random_uuid() as ""Id"",
            genre_name as ""Name""
        FROM (
            SELECT DISTINCT jsonb_array_elements_text(""Genres"") as genre_name
            FROM ""MediaItems""
            WHERE ""Genres"" IS NOT NULL 
            AND jsonb_array_length(""Genres"") > 0
        ) g
        WHERE genre_name != ''
        ON CONFLICT DO NOTHING;
    ");

    migrationBuilder.Sql(@"
        -- Create MediaItem-Topic relationships
        INSERT INTO ""MediaItemTopics"" (""MediaItemId"", ""TopicId"")
        SELECT DISTINCT 
            m.""Id"" as ""MediaItemId"",
            t.""Id"" as ""TopicId""
        FROM ""MediaItems"" m
        CROSS JOIN LATERAL jsonb_array_elements_text(m.""Topics"") as topic_name
        JOIN ""Topics"" t ON t.""Name"" = topic_name
        WHERE m.""Topics"" IS NOT NULL 
        AND jsonb_array_length(m.""Topics"") > 0
        AND topic_name != '';
    ");

    migrationBuilder.Sql(@"
        -- Create MediaItem-Genre relationships
        INSERT INTO ""MediaItemGenres"" (""MediaItemId"", ""GenreId"")
        SELECT DISTINCT 
            m.""Id"" as ""MediaItemId"",
            g.""Id"" as ""GenreId""
        FROM ""MediaItems"" m
        CROSS JOIN LATERAL jsonb_array_elements_text(m.""Genres"") as genre_name
        JOIN ""Genres"" g ON g.""Name"" = genre_name
        WHERE m.""Genres"" IS NOT NULL 
        AND jsonb_array_length(m.""Genres"") > 0
        AND genre_name != '';
    ");

    // Step 5: Drop the old JSONB columns
    migrationBuilder.DropColumn(
        name: "Topics",
        table: "MediaItems");

    migrationBuilder.DropColumn(
        name: "Genres",
        table: "MediaItems");
}
