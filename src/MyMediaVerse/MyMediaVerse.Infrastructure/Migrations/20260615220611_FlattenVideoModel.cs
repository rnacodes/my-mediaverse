using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FlattenVideoModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Videos_Videos_ParentVideoId",
                table: "Videos");

            migrationBuilder.DropForeignKey(
                name: "FK_YouTubePlaylists_YouTubeChannels_LinkedYouTubeChannelId",
                table: "YouTubePlaylists");

            migrationBuilder.DropIndex(
                name: "IX_YouTubePlaylists_LinkedYouTubeChannelId",
                table: "YouTubePlaylists");

            migrationBuilder.DropIndex(
                name: "IX_Videos_ParentVideoId",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_VideoType",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "ChannelExternalId",
                table: "YouTubePlaylists");

            migrationBuilder.DropColumn(
                name: "LinkedYouTubeChannelId",
                table: "YouTubePlaylists");

            migrationBuilder.DropColumn(
                name: "ParentVideoId",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "VideoType",
                table: "Videos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChannelExternalId",
                table: "YouTubePlaylists",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedYouTubeChannelId",
                table: "YouTubePlaylists",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentVideoId",
                table: "Videos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoType",
                table: "Videos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubePlaylists_LinkedYouTubeChannelId",
                table: "YouTubePlaylists",
                column: "LinkedYouTubeChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ParentVideoId",
                table: "Videos",
                column: "ParentVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_VideoType",
                table: "Videos",
                column: "VideoType");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_Videos_ParentVideoId",
                table: "Videos",
                column: "ParentVideoId",
                principalTable: "Videos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YouTubePlaylists_YouTubeChannels_LinkedYouTubeChannelId",
                table: "YouTubePlaylists",
                column: "LinkedYouTubeChannelId",
                principalTable: "YouTubeChannels",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
