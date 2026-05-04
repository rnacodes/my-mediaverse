# My MediaVerse v1

**A personal media library management system for tracking and organizing your entire media consumption journey.**

<!-- Add project logo here -->

> **Note:** This project was originally developed under the placeholder name "Project Loopbreaker" during development. You may see this name throughout the codebase and documentation. A complete rebrand to "My MediaVerse" is planned for a future version.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Screenshots](#screenshots)
- [Getting Started](#getting-started)
- [Architecture](#architecture)
- [API Endpoints](#api-endpoints)
- [Demo Site](#demo-site)
- [Testing](#testing)
- [Upcoming Features](#upcoming-features)
- [Project Status](#project-status)
- [Documentation](#documentation)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgments](#acknowledgments)

---

## Overview

My MediaVerse is a comprehensive personal media library management application designed to help you track, organize, and discover content across multiple media formats. Whether you're tracking books you've read, podcasts you're listening to, movies on your watchlist, or articles you've saved, My MediaVerse brings everything together in one unified platform. 

I created My MediaVerse after years of trying to organize my entertainment options, and not finding any existing tools that had the exact functionality that I wanted. I found platforms that would support specific types of media, such as movies, but none that could bring all of my options together. The placeholder name "Project Loopbreaker" partially came about because it's easy to fall into a loop of pointless phone scrolling if you can't decide what you want to do - but having everything in one place could help avoid that. If I'm bored, I can just pull up one of my mixlists for the topic I'm interested in at the time, and easily find something to read, watch, or listen to.

### Who is it for?

- **Avid media consumers** who want to track their consumption across multiple formats
- **Note-takers** who want to connect their reading highlights and notes to source material
- **Curators** who enjoy creating themed collections (mixlists) of diverse content
- **Knowledge workers** who want to integrate their Obsidian notes with their media library

---

## Features

### Media Library Management

- **8 Media Types Supported:** Books, Podcasts (shows and episodes), Movies, TV Shows, YouTube (Videos, Channels, and Playlists), Articles, Websites, and Notes
- **Status Tracking:** Mark items as Uncharted, In Progress, Consumed, or Did Not Finish
- **Rating System:** Rate content with SuperLike, Like, Neutral, or Dislike
- **Classification:** Organize with genres and topics for better discoverability
- **Personal Notes:** Add custom notes and link to external documentation
- **Ownership Tracking:** Track whether you own, want to buy, or have borrowed items

### Mixlists (Custom Collections)

Create themed playlists called "Mixlists" that can contain any combination of media types. Unlike traditional playlists, Mixlists are flexible collections based on themes, topics, or any criteria you choose.

### External API Integrations

| Service              | Functionality                                              |
| -------------------- | ---------------------------------------------------------- |
| **Readwise/Reader**  | Sync articles and highlights with automatic source linking |
| **ListenNotes**      | Search and import podcasts with comprehensive metadata     |
| **TMDB**             | Movie and TV show metadata enrichment                      |
| **YouTube Data API** | Import videos, channels, and playlists                     |
| **Google Books API** | Book search and metadata retrieval                         |

### Search & Discovery

- **Typesense-Powered Search:** Fast, typo-tolerant full-text search across your entire library
- **Advanced Filtering:** Filter by media type, topic, genre, or status
- **Combined Queries:** Search with multiple filters simultaneously
- **Real-Time Indexing Toggle:** Pause automatic per-item indexing during bulk imports, then reindex in one batch

### Highlights System

- Import highlights from your Readwise account
- Automatic linking of highlights to source articles and books
- Bulk linking functionality for associating highlights with sources

### Notes Integration

- Sync notes from your Obsidian vaults
- AI-powered description generation for notes
- Link notes to related media items in your library
- Search notes alongside other media types

### AI-Powered Features

My MediaVerse leverages AI to enhance discovery and organization:

| Feature                       | Description                                                                                                                                     |
| ----------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| **Vector Embeddings**         | Semantic embeddings generated for all media items and notes using OpenAI's text-embedding-3-large model, stored in PostgreSQL with pgvector     |
| **Similar Items**             | Discover semantically related media items and notes based on embedding similarity                                                               |
| **AI Description Generation** | Automatic concise summaries generated for notes using DigitalOcean Gradient AI                                                                  |

---

## Tech Stack

### Frontend

| Technology        | Purpose                   |
| ----------------- | ------------------------- |
| React 18          | UI framework              |
| Vite              | Build tool and dev server |
| Material-UI (MUI) | Component library         |
| React Router v7   | Client-side routing       |
| Axios             | HTTP client               |
| Vitest            | Testing framework         |

### Backend

| Technology            | Purpose               |
| --------------------- | --------------------- |
| ASP.NET Core 8.0      | Web API framework     |
| Entity Framework Core | ORM                   |
| PostgreSQL            | Primary database      |
| JWT                   | Authentication        |
| Clean Architecture    | Architectural pattern |

### Infrastructure

| Service               | Purpose                   |
| --------------------- | ------------------------- |
| Typesense             | Full-text search engine   |
| PostgreSQL + pgvector | Vector similarity search  |
| DigitalOcean Spaces   | File/thumbnail storage    |
| Render.com            | Application hosting       |
| Cloudflare            | DNS, SSL, DDoS protection |

### AI Services

| Provider                 | Purpose                                    |
| ------------------------ | ------------------------------------------ |
| OpenAI                   | Vector embeddings (text-embedding-3-large) |
| DigitalOcean Gradient AI | Text generation for note descriptions      |

---

## Screenshots

[Coming Soon]

<!-- Add screenshot: Homepage with media type icons -->

<!-- Add screenshot: Search results page -->

<!-- Add screenshot: Media profile page -->

<!-- Add screenshot: Mixlist view -->

<!-- Add screenshot: Highlight profile page -->

---

## Getting Started

### Prerequisites

- Node.js 18+
- .NET 8 SDK
- PostgreSQL 15+
- Typesense server (optional for local development)

### Frontend Setup

```bash
cd frontend
npm install
npm run dev
```

The frontend will start at `http://localhost:5173`

### Backend Setup

```bash
cd src/ProjectLoopbreaker/ProjectLoopbreaker.Web.API
dotnet build
dotnet run
```

The API will start at `http://localhost:5033`

### Environment Variables

#### Required

| Variable                                                 | Description                         |
| -------------------------------------------------------- | ----------------------------------- |
| `DATABASE_URL` or `ConnectionStrings__DefaultConnection` | PostgreSQL connection string        |
| `JWT_SECRET`                                             | JWT signing key (min 32 characters) |
| `AUTH_USERNAME`                                          | Login username                      |
| `AUTH_PASSWORD`                                          | Login password                      |
| `FRONTEND_URL`                                           | Frontend URL for CORS               |

#### Optional (API Integrations)

| Variable                        | Description              |
| ------------------------------- | ------------------------ |
| `LISTENNOTES_API_KEY`           | ListenNotes podcast API  |
| `TMDB_API_KEY`                  | TMDB movie/TV API        |
| `YOUTUBE_API_KEY`               | YouTube Data API         |
| `READWISE_API_KEY`              | Readwise/Reader API      |
| `TYPESENSE_ADMIN_API_KEY`       | Typesense search         |
| `TYPESENSE_HOST`                | Typesense server URL     |
| `DIGITALOCEANSPACES__ACCESSKEY` | DO Spaces access         |
| `DIGITALOCEANSPACES__SECRETKEY` | DO Spaces secret         |
| `OPENAI_API_KEY`                | OpenAI embeddings        |
| `GRADIENT_API_KEY`              | DigitalOcean Gradient AI |

#### Local-only (migration script)

These are read **only** by `scripts/run-migrations.ps1` from your local shell. They are never read by deployed app code and should not be set on Render or the demo Droplet.

| Variable                   | Description                                                                  |
| -------------------------- | ---------------------------------------------------------------------------- |
| `PRODUCTION_DB_CONNECTION` | Connection string for the production DB (Render)                             |
| `DEMO_DB_CONNECTION`       | Connection string for the demo DB via SSH tunnel (`Host=localhost;Port=5433;...`) |
| `DEMO_DROPLET_IP`          | IP of the DigitalOcean Droplet hosting the demo DB                           |

---

## Architecture

My MediaVerse follows **Clean Architecture** principles with a layered structure that promotes separation of concerns and testability.

```
┌─────────────────────────────────────┐
│           Web.API Layer             │  Controllers, Middleware, Filters
├─────────────────────────────────────┤
│         Application Layer           │  Services, Business Logic, Interfaces
├─────────────────────────────────────┤
│           Domain Layer              │  Entities, Domain Models
├─────────────────────────────────────┤
│       Infrastructure Layer          │  EF Core, External API Clients
├─────────────────────────────────────┤
│           Shared Layer              │  DTOs, Utilities, Common Interfaces
└─────────────────────────────────────┘
```

### Key Architectural Patterns

- **Clean Architecture:** Dependencies flow inward, with the domain at the center
- **Domain-Driven Design:** Rich domain models with business logic
- **SOLID Principles:** Single responsibility, dependency injection throughout
- **Repository Pattern:** Abstracted data access
- **Service Layer:** Business logic separated from controllers

---

## API Endpoints

The API is RESTful with a base URL of `/api`. Key controllers include:

| Controller     | Endpoint              | Description                              |
| -------------- | --------------------- | ---------------------------------------- |
| Media          | `/api/media`          | CRUD operations for all media types      |
| Mixlist        | `/api/mixlist`        | Collection management                    |
| Highlights     | `/api/highlight`      | Highlight operations and linking         |
| Search         | `/api/search`         | Typesense search integration             |
| Readwise       | `/api/readwise`       | Readwise sync operations                 |
| Podcast        | `/api/podcast`        | Podcast-specific operations              |
| Book           | `/api/book`           | Book-specific operations                 |
| Movie          | `/api/movie`          | Movie operations with TMDB               |
| TVShow         | `/api/tvshow`         | TV show operations                       |
| YouTube        | `/api/youtube`        | Video/channel/playlist management        |
| Topics         | `/api/topics`         | Topic management                         |
| Genres         | `/api/genres`         | Genre management                         |
| Notes          | `/api/note`           | Notes management                         |
| AI             | `/api/ai`             | Embedding generation and AI descriptions |
| Recommendation | `/api/recommendation` | Similar items and personalized discovery |

---

## Demo Site

**Live Demo:** [https://demo.mymediaverseuniverse.com](https://demo.mymediaverseuniverse.com)

The demo site runs in **read-only mode** by default, allowing you to explore the interface and features without modifying data. Sample data is included to demonstrate all media types and functionality.

---

## Testing

### Run All Tests

```powershell
.\run-all-tests.ps1
```

### Backend Tests Only

```bash
dotnet test src/ProjectLoopbreaker/ProjectLoopbreaker.sln
```

### Frontend Tests Only

```bash
cd frontend
npm test        # Watch mode
npm run test:run    # Single run
npm run test:coverage   # With coverage
```

Test results are saved to the `logs/` directory with timestamps.

---

## Upcoming Features

Future releases will include:

* **Support for additional media types:**
  
  * Music
  * Courses
  * Video Games
  * Panorama: a custom media type for anything that doesn't fit neatly into an existing category

* **Integration with self-hosted media services** to manage actual media files. Services under consideration include:
  
  * Audiobookshelf for podcasts and audiobooks
  * Paperless-ngx for documents
  * Calibre-Web for eBooks
  * Navidrome for music

* **Automated metadata enrichment** via scheduled N8N jobs

* **Webpage archival** using ArchiveBox

* **Podcast and video transcription import**

---

## Project Status

**Current Version:** v1 (MVP)

This is the Minimum Viable Product release with core functionality complete. Active development is ongoing with additional features and polish planned for future versions.

### MVP Completion Status

- Media library management for all 8 types
- Mixlist creation and management
- Typesense search integration
- AI-powered recommendations
- Vector embeddings for semantic discovery
- Readwise article and highlight sync
- Notes integration with Obsidian
- Topic and genre classification
- Demo site with sample data

---

## Documentation

For detailed documentation, including architecture deep-dives and code examples, see:

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/rnacodes/my-mediaverse)

---

## Contributing

This is a personal project, but feedback and suggestions are welcome. Please open an issue on GitHub to:

- Report bugs
- Suggest features
- Ask questions

---

## License

**Proprietary License**

> Please do not copy or redistribute this code without permission.

This project is not open source. All rights reserved.

---

## Acknowledgments

This project integrates with and is grateful for the following services and APIs:

- [ListenNotes](https://www.listennotes.com/) - Podcast search API
- [TMDB](https://www.themoviedb.org/) - Movie and TV database
- [YouTube Data API](https://developers.google.com/youtube/v3) - Video platform integration
- [Readwise](https://readwise.io/) - Reading highlights service
- [Google Books API](https://developers.google.com/books) - Book metadata
- [Typesense](https://typesense.org/) - Open source search engine
- [OpenAI](https://openai.com/) - Embedding models for semantic search
- [Material-UI](https://mui.com/) - React component library

---

*Built with care for personal media management.*
