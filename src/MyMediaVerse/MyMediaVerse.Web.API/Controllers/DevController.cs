using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Infrastructure;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevController : ControllerBase
    {
        private readonly Infrastructure.Data.MediaLibraryDbContext _context;
        private readonly ILogger<DevController> _logger;
        private readonly IWebHostEnvironment _environment;

        public DevController(
            Infrastructure.Data.MediaLibraryDbContext context,
            ILogger<DevController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        // Helper method to check if the current environment allows dev operations
        private IActionResult? CheckEnvironment()
        {
            if (_environment.IsProduction())
            {
                _logger.LogWarning("Attempted to access DevController in production environment");
                return StatusCode(403, new
                {
                    error = "DevController endpoints are disabled in production",
                    message = "These endpoints are only available in Development and Staging environments for security reasons."
                });
            }
            return null;
        }

        // POST: api/dev/reset-database
        [HttpPost("reset-database")]
        public async Task<IActionResult> ResetDatabase()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();
                return Ok("Database reset successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to reset database", details = ex.Message });
            }
        }

        // POST: api/dev/seed-mixlists
        [HttpPost("seed-mixlists")]
        public async Task<IActionResult> SeedMixlists()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                var sampleMixlists = new List<Mixlist>
                {
                    new Mixlist
                    {
                        Name = "Cyberpunk Dystopia",
                        Description = "Neon-drenched streets and high-tech, low-life stories.",
                        Thumbnail = "https://placehold.co/600x400/1B1B1B/362759.png"
                    },
                    new Mixlist
                    {
                        Name = "Ancient Empires",
                        Description = "The rise and fall of civilizations, from Rome to the Nile.",
                        Thumbnail = "https://placehold.co/600x400/474350/fcfafa.png"
                    },
                    new Mixlist
                    {
                        Name = "Cosmic Wonders",
                        Description = "Explore black holes, distant galaxies, and the mysteries of space.",
                        Thumbnail = "https://placehold.co/600x400/300a70/fcfafa.png"
                    },
                    new Mixlist
                    {
                        Name = "Mindful Moments",
                        Description = "Podcasts and music for focus, meditation, and calm.",
                        Thumbnail = "https://placehold.co/600x400/362759/1B1B1B.png"
                    },
                    new Mixlist
                    {
                        Name = "Fantasy Realms",
                        Description = "Epic quests, magical creatures, and worlds beyond imagination.",
                        Thumbnail = "https://placehold.co/600x400/1E1E1E/300a70.png"
                    },
                    new Mixlist
                    {
                        Name = "Code & Coffee",
                        Description = "Deep work mixlists and tech podcasts to fuel your projects.",
                        Thumbnail = "https://placehold.co/600x400/fcfafa/474350.png"
                    }
                };

                _context.Mixlists.AddRange(sampleMixlists);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Sample mixlists added successfully", count = sampleMixlists.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to add sample mixlists", details = ex.Message });
            }
        }

        // POST: api/dev/seed-demo-data
        [HttpPost("seed-demo-data")]
        public async Task<IActionResult> SeedDemoData()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting demo data seeding...");

                // Create Topics
                var topics = new List<Topic>
                {
                    new Topic { Name = "technology" },
                    new Topic { Name = "science" },
                    new Topic { Name = "philosophy" },
                    new Topic { Name = "history" },
                    new Topic { Name = "psychology" }
                };
                _context.Topics.AddRange(topics);
                await _context.SaveChangesAsync();

                // Create Genres
                var genres = new List<Genre>
                {
                    new Genre { Name = "sci-fi" },
                    new Genre { Name = "documentary" },
                    new Genre { Name = "non-fiction" },
                    new Genre { Name = "tutorial" },
                    new Genre { Name = "educational" }
                };
                _context.Genres.AddRange(genres);
                await _context.SaveChangesAsync();

                // Create Books
                var book1 = new Book
                {
                    Title = "The Hitchhiker's Guide to the Galaxy",
                    MediaType = MediaType.Book,
                    Status = Status.Completed,
                    DateAdded = DateTime.UtcNow.AddDays(-30),
                    Rating = Rating.Like,
                    Description = "A hilarious science fiction comedy about the end of Earth and interstellar adventures.",
                    Thumbnail = "https://placehold.co/300x450/474350/fcfafa?text=Hitchhiker",
                    Author = "Douglas Adams",
                    Topics = new List<Topic> { topics[1] }, // science
                    Genres = new List<Genre> { genres[0] }  // sci-fi
                };

                var book2 = new Book
                {
                    Title = "Sapiens: A Brief History of Humankind",
                    MediaType = MediaType.Book,
                    Status = Status.Completed,
                    DateAdded = DateTime.UtcNow.AddDays(-60),
                    Rating = Rating.Like,
                    Description = "A thought-provoking exploration of human history from the Stone Age to modern times.",
                    Thumbnail = "https://placehold.co/300x450/474350/fcfafa?text=Sapiens",
                    Author = "Yuval Noah Harari",
                    Topics = new List<Topic> { topics[3], topics[2] }, // history, philosophy
                    Genres = new List<Genre> { genres[2] }  // non-fiction
                };

                _context.Books.AddRange(new[] { book1, book2 });
                await _context.SaveChangesAsync();

                // Create Movies
                var movie1 = new Movie
                {
                    Title = "Inception",
                    MediaType = MediaType.Movie,
                    Status = Status.Completed,
                    DateAdded = DateTime.UtcNow.AddDays(-45),
                    Rating = Rating.Like,
                    Description = "A mind-bending thriller about dreams within dreams and corporate espionage.",
                    Thumbnail = "https://placehold.co/300x450/474350/fcfafa?text=Inception",
                    Director = "Christopher Nolan",
                    ReleaseYear = 2010,
                    Topics = new List<Topic> { topics[4] }, // psychology
                    Genres = new List<Genre> { genres[0] }  // sci-fi
                };

                var movie2 = new Movie
                {
                    Title = "The Matrix",
                    MediaType = MediaType.Movie,
                    Status = Status.Completed,
                    DateAdded = DateTime.UtcNow.AddDays(-90),
                    Rating = Rating.Like,
                    Description = "A groundbreaking sci-fi film about reality, AI, and human consciousness.",
                    Thumbnail = "https://placehold.co/300x450/474350/fcfafa?text=Matrix",
                    Director = "The Wachowskis",
                    ReleaseYear = 1999,
                    Topics = new List<Topic> { topics[0], topics[2] }, // technology, philosophy
                    Genres = new List<Genre> { genres[0] }  // sci-fi
                };

                _context.Movies.AddRange(new[] { movie1, movie2 });
                await _context.SaveChangesAsync();

                // Create Videos
                var video1 = new Video
                {
                    Title = "How AI Will Change The World",
                    MediaType = MediaType.Video,
                    Status = Status.ActivelyExploring,
                    DateAdded = DateTime.UtcNow.AddDays(-10),
                    Description = "An in-depth exploration of artificial intelligence and its impact on society.",
                    Thumbnail = "https://placehold.co/300x450/474350/fcfafa?text=AI+Future",
                    Link = "https://www.youtube.com/watch?v=demo1",
                    Platform = "YouTube",
                    Topics = new List<Topic> { topics[0] }, // technology
                    Genres = new List<Genre> { genres[1], genres[4] }  // documentary, educational
                };

                var video2 = new Video
                {
                    Title = "The History of Computing",
                    MediaType = MediaType.Video,
                    Status = Status.ActivelyExploring,
                    DateAdded = DateTime.UtcNow.AddDays(-15),
                    Rating = Rating.Neutral,
                    Description = "A fascinating documentary about the evolution of computers from ENIAC to modern smartphones.",
                    Thumbnail = "https://placehold.co/300x450/474350/fcfafa?text=Computing",
                    Link = "https://www.youtube.com/watch?v=demo2",
                    Platform = "YouTube",
                    Topics = new List<Topic> { topics[0], topics[3] }, // technology, history
                    Genres = new List<Genre> { genres[1] }  // documentary
                };

                _context.Videos.AddRange(new[] { video1, video2 });
                await _context.SaveChangesAsync();

                // Create Articles
                var article1 = new Article
                {
                    Title = "The Future of Work in the Age of AI",
                    MediaType = MediaType.Article,
                    Status = Status.Uncharted,
                    DateAdded = DateTime.UtcNow.AddDays(-5),
                    Description = "An analysis of how artificial intelligence will reshape the job market and work culture.",
                    Thumbnail = "https://placehold.co/300x450/474350/fcfafa?text=Future+Work",
                    Link = "https://example.com/future-of-work",
                    Author = "Tech Insights",
                    Topics = new List<Topic> { topics[0] }, // technology
                    Genres = new List<Genre> { genres[2] }  // non-fiction
                };

                var article2 = new Article
                {
                    Title = "Understanding Quantum Computing",
                    MediaType = MediaType.Article,
                    Status = Status.ActivelyExploring,
                    DateAdded = DateTime.UtcNow.AddDays(-20),
                    Rating = Rating.Neutral,
                    Description = "A beginner-friendly introduction to quantum computing principles and applications.",
                    Thumbnail = "https://placehold.co/300x450/474350/fcfafa?text=Quantum",
                    Link = "https://example.com/quantum-computing",
                    Author = "Science Today",
                    Topics = new List<Topic> { topics[0], topics[1] }, // technology, science
                    Genres = new List<Genre> { genres[4] }  // educational
                };

                _context.Articles.AddRange(new[] { article1, article2 });
                await _context.SaveChangesAsync();

                // Create Mixlists with media
                var mixlist1 = new Mixlist
                {
                    Name = "Mind-Bending Sci-Fi",
                    Description = "A collection of thought-provoking science fiction that will make you question reality.",
                    DateCreated = DateTime.UtcNow.AddDays(-25),
                    Thumbnail = "https://placehold.co/600x400/474350/fcfafa?text=Sci-Fi+Collection",
                    MediaItems = new List<BaseMediaItem> { book1, movie1, movie2 }
                };

                var mixlist2 = new Mixlist
                {
                    Name = "Learn About Technology",
                    Description = "Educational content about technology, AI, and computing for curious minds.",
                    DateCreated = DateTime.UtcNow.AddDays(-15),
                    Thumbnail = "https://placehold.co/600x400/474350/fcfafa?text=Tech+Learning",
                    MediaItems = new List<BaseMediaItem> { video1, video2, article1, article2 }
                };

                var mixlist3 = new Mixlist
                {
                    Name = "Currently Exploring",
                    Description = "Media items I'm actively going through right now.",
                    DateCreated = DateTime.UtcNow.AddDays(-7),
                    Thumbnail = "https://placehold.co/600x400/474350/fcfafa?text=Active",
                    MediaItems = new List<BaseMediaItem> { video1, article2 }
                };

                _context.Mixlists.AddRange(new[] { mixlist1, mixlist2, mixlist3 });
                await _context.SaveChangesAsync();

                _logger.LogInformation("Demo data seeded successfully!");

                return Ok(new
                {
                    message = "Demo data seeded successfully!",
                    created = new
                    {
                        topics = topics.Count,
                        genres = genres.Count,
                        books = 2,
                        movies = 2,
                        videos = 2,
                        articles = 2,
                        mixlists = 3
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding demo data");
                return StatusCode(500, new { error = "Failed to seed demo data", details = ex.Message });
            }
        }

        // POST: api/dev/seed-demo-notes
        [HttpPost("seed-demo-notes")]
        public async Task<IActionResult> SeedDemoNotes()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting demo notes seeding...");

                // Create sample notes
                var notes = new List<Note>
                {
                    new Note
                    {
                        Slug = "my-productivity-system",
                        Title = "My Productivity System",
                        Content = "# My Productivity System\n\nOver the years, I've developed a productivity system that works for me...\n\n## Core Principles\n\n1. **Time Blocking** - Schedule specific blocks for deep work\n2. **Weekly Reviews** - Reflect on progress every Sunday\n3. **Capture Everything** - Use a trusted inbox for all ideas\n\n## Tools I Use\n\n- Obsidian for notes\n- Todoist for tasks\n- Calendar for time blocking\n\n## Key Insights from Books\n\nYuval Harari's Sapiens taught me that our ability to cooperate in large numbers is what sets us apart. This applies to productivity too - building systems that scale.",
                        Description = "A comprehensive overview of my personal productivity system and the principles behind it.",
                        VaultName = "general",
                        SourceUrl = "https://garden.mymediaverseuniverse.com/my-productivity-system",
                        Tags = new List<string> { "productivity", "systems", "habits" },
                        NoteDate = DateTime.UtcNow.AddDays(-30),
                        DateImported = DateTime.UtcNow,
                        ContentHash = Guid.NewGuid().ToString("N").Substring(0, 64)
                    },
                    new Note
                    {
                        Slug = "learning-resources-workflow",
                        Title = "Learning Resources Workflow",
                        Content = "# Learning Resources Workflow\n\nHow I process and retain information from various media sources.\n\n## The Capture Phase\n\n- Watch/read with intention\n- Take quick notes during consumption\n- Mark highlights and timestamps\n\n## The Process Phase\n\n- Review notes within 24 hours\n- Connect to existing knowledge\n- Create atomic notes\n\n## AI and Learning\n\nAI tools are changing how we learn. I recently watched a great video about how AI will transform education and knowledge work.",
                        Description = "My workflow for capturing, processing, and retaining knowledge from videos, articles, and books.",
                        VaultName = "general",
                        SourceUrl = "https://garden.mymediaverseuniverse.com/learning-resources-workflow",
                        Tags = new List<string> { "learning", "workflow", "knowledge-management" },
                        NoteDate = DateTime.UtcNow.AddDays(-20),
                        DateImported = DateTime.UtcNow,
                        ContentHash = Guid.NewGuid().ToString("N").Substring(0, 64)
                    },
                    new Note
                    {
                        Slug = "dotnet-dependency-injection-deep-dive",
                        Title = ".NET Dependency Injection Deep Dive",
                        Content = "# .NET Dependency Injection Deep Dive\n\n## Service Lifetimes\n\n### Transient\n- Created each time requested\n- Best for lightweight, stateless services\n\n### Scoped\n- One instance per request/scope\n- Ideal for DbContext and request-specific data\n\n### Singleton\n- One instance for entire app lifetime\n- Use for expensive-to-create services\n\n## Best Practices\n\n```csharp\n// Good: Interface segregation\nbuilder.Services.AddScoped<IUserRepository, UserRepository>();\n\n// Avoid: Registering implementations directly\nbuilder.Services.AddScoped<UserRepository>();\n```\n\n## Common Pitfalls\n\n1. Captive dependencies\n2. Service locator anti-pattern\n3. Circular dependencies",
                        Description = "A comprehensive guide to dependency injection in .NET, covering service lifetimes, best practices, and common pitfalls.",
                        VaultName = "programming",
                        SourceUrl = "https://hackerman.mymediaverseuniverse.com/dotnet-dependency-injection-deep-dive",
                        Tags = new List<string> { "dotnet", "dependency-injection", "architecture", "csharp" },
                        NoteDate = DateTime.UtcNow.AddDays(-15),
                        DateImported = DateTime.UtcNow,
                        ContentHash = Guid.NewGuid().ToString("N").Substring(0, 64)
                    },
                    new Note
                    {
                        Slug = "react-state-management-patterns",
                        Title = "React State Management Patterns",
                        Content = "# React State Management Patterns\n\n## Local State\n\n```jsx\nconst [count, setCount] = useState(0);\n```\n\nBest for component-specific UI state.\n\n## Context API\n\n```jsx\nconst ThemeContext = createContext();\n\nfunction App() {\n  return (\n    <ThemeContext.Provider value=\"dark\">\n      <Page />\n    </ThemeContext.Provider>\n  );\n}\n```\n\nGood for theme, auth, and other cross-cutting concerns.\n\n## When to Use Redux/Zustand\n\n- Complex state logic\n- State shared across many components\n- Need for time-travel debugging\n\n## My Recommendations\n\n1. Start with local state\n2. Lift state up when needed\n3. Use Context for truly global state\n4. Consider Zustand for complex apps",
                        Description = "Comparing different state management approaches in React, from useState to Context to external libraries.",
                        VaultName = "programming",
                        SourceUrl = "https://hackerman.mymediaverseuniverse.com/react-state-management-patterns",
                        Tags = new List<string> { "react", "javascript", "state-management", "frontend" },
                        NoteDate = DateTime.UtcNow.AddDays(-10),
                        DateImported = DateTime.UtcNow,
                        ContentHash = Guid.NewGuid().ToString("N").Substring(0, 64)
                    },
                    new Note
                    {
                        Slug = "api-design-best-practices",
                        Title = "API Design Best Practices",
                        Content = "# API Design Best Practices\n\n## RESTful Principles\n\n- Use nouns for resources: `/users`, `/articles`\n- HTTP verbs for actions: GET, POST, PUT, DELETE\n- Consistent naming conventions\n\n## Pagination\n\n```json\n{\n  \"data\": [...],\n  \"meta\": {\n    \"page\": 1,\n    \"perPage\": 20,\n    \"total\": 100\n  }\n}\n```\n\n## Error Handling\n\n```json\n{\n  \"error\": \"ValidationError\",\n  \"message\": \"Email is required\",\n  \"details\": {\n    \"field\": \"email\"\n  }\n}\n```\n\n## Versioning Strategies\n\n1. URL versioning: `/api/v1/users`\n2. Header versioning: `Accept: application/vnd.api.v1+json`\n3. Query parameter: `/api/users?version=1`",
                        Description = "Guidelines for designing clean, consistent, and developer-friendly REST APIs.",
                        VaultName = "programming",
                        SourceUrl = "https://hackerman.mymediaverseuniverse.com/api-design-best-practices",
                        Tags = new List<string> { "api", "rest", "backend", "architecture" },
                        NoteDate = DateTime.UtcNow.AddDays(-5),
                        DateImported = DateTime.UtcNow,
                        ContentHash = Guid.NewGuid().ToString("N").Substring(0, 64)
                    }
                };

                _context.Notes.AddRange(notes);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created {Count} demo notes", notes.Count);

                // Try to link notes to existing media items
                var linksCreated = 0;

                // Link "My Productivity System" to Sapiens book
                var sapiensBook = await _context.Books
                    .FirstOrDefaultAsync(b => b.Title.Contains("Sapiens"));
                if (sapiensBook != null)
                {
                    var productivityNote = notes.First(n => n.Slug == "my-productivity-system");
                    var link = new MediaItemNote
                    {
                        NoteId = productivityNote.Id,
                        MediaItemId = sapiensBook.Id,
                        LinkDescription = "Key insights from Sapiens applied to productivity",
                        LinkedAt = DateTime.UtcNow
                    };
                    _context.Set<MediaItemNote>().Add(link);
                    linksCreated++;
                    _logger.LogInformation("Linked 'My Productivity System' to 'Sapiens'");
                }

                // Link "Learning Resources Workflow" to AI video
                var aiVideo = await _context.Videos
                    .FirstOrDefaultAsync(v => v.Title.Contains("AI"));
                if (aiVideo != null)
                {
                    var learningNote = notes.First(n => n.Slug == "learning-resources-workflow");
                    var link = new MediaItemNote
                    {
                        NoteId = learningNote.Id,
                        MediaItemId = aiVideo.Id,
                        LinkDescription = "Referenced in learning workflow discussion",
                        LinkedAt = DateTime.UtcNow
                    };
                    _context.Set<MediaItemNote>().Add(link);
                    linksCreated++;
                    _logger.LogInformation("Linked 'Learning Resources Workflow' to AI video");
                }

                // Link ".NET Dependency Injection" to tech articles
                var techArticle = await _context.Articles
                    .FirstOrDefaultAsync(a => a.Title.Contains("Future") || a.Title.Contains("Tech"));
                if (techArticle != null)
                {
                    var diNote = notes.First(n => n.Slug == "dotnet-dependency-injection-deep-dive");
                    var link = new MediaItemNote
                    {
                        NoteId = diNote.Id,
                        MediaItemId = techArticle.Id,
                        LinkDescription = "Related technology concepts",
                        LinkedAt = DateTime.UtcNow
                    };
                    _context.Set<MediaItemNote>().Add(link);
                    linksCreated++;
                    _logger.LogInformation("Linked '.NET DI Deep Dive' to tech article");
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Demo notes seeded successfully! Run POST /api/search/reindex-notes to index them in Typesense.");

                return Ok(new
                {
                    message = "Demo notes seeded successfully! Run POST /api/search/reindex-notes to index them in Typesense.",
                    created = new
                    {
                        notes = notes.Count,
                        generalVault = notes.Count(n => n.VaultName == "general"),
                        programmingVault = notes.Count(n => n.VaultName == "programming"),
                        mediaItemLinks = linksCreated
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding demo notes");
                return StatusCode(500, new { error = "Failed to seed demo notes", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-notes
        [HttpPost("cleanup-notes")]
        public async Task<IActionResult> CleanupNotes()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting notes data cleanup...");

                // Delete note-media links first
                var noteLinks = await _context.Set<MediaItemNote>().ToListAsync();
                _context.Set<MediaItemNote>().RemoveRange(noteLinks);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} note-media links", noteLinks.Count);

                // Delete all notes
                var notes = await _context.Notes.ToListAsync();
                _context.Notes.RemoveRange(notes);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} notes", notes.Count);

                return Ok(new
                {
                    message = "Notes cleanup completed successfully",
                    deleted = new
                    {
                        noteMediaLinks = noteLinks.Count,
                        notes = notes.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during notes cleanup");
                return StatusCode(500, new { error = "Failed to cleanup notes", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-youtube-data
        [HttpPost("cleanup-youtube-data")]
        public async Task<IActionResult> CleanupYouTubeData()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting YouTube data cleanup...");

                // Step 1: Delete all playlist-video associations
                var playlistVideos = await _context.Set<YouTubePlaylistVideo>().ToListAsync();
                _context.Set<YouTubePlaylistVideo>().RemoveRange(playlistVideos);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {playlistVideos.Count} playlist-video associations");

                // Step 2: Delete all YouTube playlists
                var playlists = await _context.YouTubePlaylists.ToListAsync();
                _context.YouTubePlaylists.RemoveRange(playlists);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {playlists.Count} YouTube playlists");

                // Step 3: Delete all videos
                var videos = await _context.Videos.ToListAsync();
                _context.Videos.RemoveRange(videos);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {videos.Count} videos");

                // Step 4: Delete all YouTube channels
                var channels = await _context.YouTubeChannels.ToListAsync();
                _context.YouTubeChannels.RemoveRange(channels);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {channels.Count} YouTube channels");

                return Ok(new 
                { 
                    message = "YouTube data cleanup completed successfully",
                    deleted = new 
                    {
                        playlistVideoAssociations = playlistVideos.Count,
                        playlists = playlists.Count,
                        videos = videos.Count,
                        channels = channels.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during YouTube data cleanup");
                return StatusCode(500, new { error = "Failed to cleanup YouTube data", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-podcasts
        [HttpPost("cleanup-podcasts")]
        public async Task<IActionResult> CleanupPodcasts()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting podcast data cleanup...");

                // EF Core will handle cascade deletes based on configured relationships
                var episodes = await _context.PodcastEpisodes.ToListAsync();
                _context.PodcastEpisodes.RemoveRange(episodes);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {episodes.Count} podcast episodes");

                var series = await _context.PodcastSeries.ToListAsync();
                _context.PodcastSeries.RemoveRange(series);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {series.Count} podcast series");

                return Ok(new
                {
                    message = "Podcast data cleanup completed successfully",
                    deleted = new
                    {
                        episodes = episodes.Count,
                        series = series.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during podcast data cleanup");
                return StatusCode(500, new { error = "Failed to cleanup podcast data", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-books
        [HttpPost("cleanup-books")]
        public async Task<IActionResult> CleanupBooks()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting books data cleanup...");

                var books = await _context.Books.ToListAsync();
                _context.Books.RemoveRange(books);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {books.Count} books");

                return Ok(new
                {
                    message = "Books cleanup completed successfully",
                    deleted = new
                    {
                        books = books.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during books cleanup");
                return StatusCode(500, new { error = "Failed to cleanup books", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-movies
        [HttpPost("cleanup-movies")]
        public async Task<IActionResult> CleanupMovies()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting movies data cleanup...");

                var movies = await _context.Movies.ToListAsync();
                _context.Movies.RemoveRange(movies);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {movies.Count} movies");

                return Ok(new
                {
                    message = "Movies cleanup completed successfully",
                    deleted = new
                    {
                        movies = movies.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during movies cleanup");
                return StatusCode(500, new { error = "Failed to cleanup movies", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-tvshows
        [HttpPost("cleanup-tvshows")]
        public async Task<IActionResult> CleanupTvShows()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting TV shows data cleanup...");

                var tvShows = await _context.TvShows.ToListAsync();
                _context.TvShows.RemoveRange(tvShows);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {tvShows.Count} TV shows");

                return Ok(new
                {
                    message = "TV shows cleanup completed successfully",
                    deleted = new
                    {
                        tvShows = tvShows.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TV shows cleanup");
                return StatusCode(500, new { error = "Failed to cleanup TV shows", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-articles
        [HttpPost("cleanup-articles")]
        public async Task<IActionResult> CleanupArticles()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting articles data cleanup...");

                var articles = await _context.Articles.ToListAsync();
                _context.Articles.RemoveRange(articles);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {articles.Count} articles");

                return Ok(new
                {
                    message = "Articles cleanup completed successfully",
                    deleted = new
                    {
                        articles = articles.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during articles cleanup");
                return StatusCode(500, new { error = "Failed to cleanup articles", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-highlights
        [HttpPost("cleanup-highlights")]
        public async Task<IActionResult> CleanupHighlights()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting highlights data cleanup...");

                var highlights = await _context.Highlights.ToListAsync();
                _context.Highlights.RemoveRange(highlights);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {highlights.Count} highlights");

                return Ok(new
                {
                    message = "Highlights cleanup completed successfully",
                    deleted = new
                    {
                        highlights = highlights.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during highlights cleanup");
                return StatusCode(500, new { error = "Failed to cleanup highlights", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-mixlists
        [HttpPost("cleanup-mixlists")]
        public async Task<IActionResult> CleanupMixlists()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting mixlists data cleanup...");

                var mixlists = await _context.Mixlists.ToListAsync();
                _context.Mixlists.RemoveRange(mixlists);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {mixlists.Count} mixlists");

                return Ok(new
                {
                    message = "Mixlists cleanup completed successfully",
                    deleted = new
                    {
                        mixlists = mixlists.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during mixlists cleanup");
                return StatusCode(500, new { error = "Failed to cleanup mixlists", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-all-topics
        [HttpPost("cleanup-all-topics")]
        public async Task<IActionResult> CleanupAllTopics()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting ALL topics cleanup...");

                var topics = await _context.Topics.ToListAsync();
                _context.Topics.RemoveRange(topics);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {topics.Count} topics");

                return Ok(new
                {
                    message = "All topics cleanup completed successfully. Media items remain without topic associations.",
                    deleted = new
                    {
                        topics = topics.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during all topics cleanup");
                return StatusCode(500, new { error = "Failed to cleanup all topics", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-all-genres
        [HttpPost("cleanup-all-genres")]
        public async Task<IActionResult> CleanupAllGenres()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting ALL genres cleanup...");

                var genres = await _context.Genres.ToListAsync();
                _context.Genres.RemoveRange(genres);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {genres.Count} genres");

                return Ok(new
                {
                    message = "All genres cleanup completed successfully. Media items remain without genre associations.",
                    deleted = new
                    {
                        genres = genres.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during all genres cleanup");
                return StatusCode(500, new { error = "Failed to cleanup all genres", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-orphaned-topics
        [HttpPost("cleanup-orphaned-topics")]
        public async Task<IActionResult> CleanupOrphanedTopics()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting orphaned topics cleanup...");

                // Get all topic IDs that are still referenced in MediaItemTopics
                var referencedTopicIds = await _context.Set<Dictionary<string, object>>("MediaItemTopics")
                    .Select(x => (Guid)x["TopicId"])
                    .Distinct()
                    .ToListAsync();

                // Get topics that are NOT referenced
                var orphanedTopics = await _context.Topics
                    .Where(t => !referencedTopicIds.Contains(t.Id))
                    .ToListAsync();

                _context.Topics.RemoveRange(orphanedTopics);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {orphanedTopics.Count} orphaned topics");

                return Ok(new
                {
                    message = "Orphaned topics cleanup completed successfully",
                    deleted = new
                    {
                        topics = orphanedTopics.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during orphaned topics cleanup");
                return StatusCode(500, new { error = "Failed to cleanup orphaned topics", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-orphaned-genres
        [HttpPost("cleanup-orphaned-genres")]
        public async Task<IActionResult> CleanupOrphanedGenres()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting orphaned genres cleanup...");

                // Get all genre IDs that are still referenced in MediaItemGenres
                var referencedGenreIds = await _context.Set<Dictionary<string, object>>("MediaItemGenres")
                    .Select(x => (Guid)x["GenreId"])
                    .Distinct()
                    .ToListAsync();

                // Get genres that are NOT referenced
                var orphanedGenres = await _context.Genres
                    .Where(g => !referencedGenreIds.Contains(g.Id))
                    .ToListAsync();

                _context.Genres.RemoveRange(orphanedGenres);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {orphanedGenres.Count} orphaned genres");

                return Ok(new
                {
                    message = "Orphaned genres cleanup completed successfully",
                    deleted = new
                    {
                        genres = orphanedGenres.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during orphaned genres cleanup");
                return StatusCode(500, new { error = "Failed to cleanup orphaned genres", details = ex.Message });
            }
        }

        // GET: api/dev/diagnose-orphaned-media
        [HttpGet("diagnose-orphaned-media")]
        public async Task<IActionResult> DiagnoseOrphanedMedia()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Diagnosing orphaned media items...");

                // Use raw SQL to find media items that don't exist in any derived table
                // This finds rows in MediaItems that have no corresponding entry in type-specific tables
                var orphanedMediaQuery = @"
                    SELECT m.""Id"", m.""Title"", m.""MediaType"" 
                    FROM ""MediaItems"" m
                    WHERE NOT EXISTS (SELECT 1 FROM ""Books"" b WHERE b.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""Videos"" v WHERE v.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""Movies"" mov WHERE mov.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""TvShows"" tv WHERE tv.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""Articles"" a WHERE a.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""PodcastSeries"" ps WHERE ps.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""PodcastEpisodes"" pe WHERE pe.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""YouTubeChannels"" yc WHERE yc.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""Websites"" w WHERE w.""Id"" = m.""Id"")";

                var orphanedMedia = new List<object>();
                
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = orphanedMediaQuery;
                    await _context.Database.OpenConnectionAsync();
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orphanedMedia.Add(new
                            {
                                id = reader.GetGuid(0),
                                title = reader.IsDBNull(1) ? null : reader.GetString(1),
                                mediaType = reader.IsDBNull(2) ? null : reader.GetString(2)
                            });
                        }
                    }
                }

                return Ok(new
                {
                    message = $"Found {orphanedMedia.Count} orphaned media items that cannot be materialized",
                    orphanedCount = orphanedMedia.Count,
                    orphanedItems = orphanedMedia
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error diagnosing orphaned media");
                return StatusCode(500, new { error = "Failed to diagnose orphaned media", details = ex.Message });
            }
        }

        // POST: api/dev/fix-orphaned-media
        [HttpPost("fix-orphaned-media")]
        public async Task<IActionResult> FixOrphanedMedia()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Fixing orphaned media items...");

                // First, delete any join table references to orphaned media items
                var deleteJoinTablesQuery = @"
                    DELETE FROM ""MediaItemTopics"" 
                    WHERE ""MediaItemId"" IN (
                        SELECT m.""Id"" FROM ""MediaItems"" m
                        WHERE NOT EXISTS (SELECT 1 FROM ""Books"" b WHERE b.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""Videos"" v WHERE v.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""Movies"" mov WHERE mov.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""TvShows"" tv WHERE tv.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""Articles"" a WHERE a.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""PodcastSeries"" ps WHERE ps.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""PodcastEpisodes"" pe WHERE pe.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""YouTubeChannels"" yc WHERE yc.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""Websites"" w WHERE w.""Id"" = m.""Id"")
                    );
                    
                    DELETE FROM ""MediaItemGenres"" 
                    WHERE ""MediaItemId"" IN (
                        SELECT m.""Id"" FROM ""MediaItems"" m
                        WHERE NOT EXISTS (SELECT 1 FROM ""Books"" b WHERE b.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""Videos"" v WHERE v.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""Movies"" mov WHERE mov.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""TvShows"" tv WHERE tv.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""Articles"" a WHERE a.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""PodcastSeries"" ps WHERE ps.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""PodcastEpisodes"" pe WHERE pe.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""YouTubeChannels"" yc WHERE yc.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""Websites"" w WHERE w.""Id"" = m.""Id"")
                    );
                    
                    DELETE FROM ""MixlistMediaItems"" 
                    WHERE ""MediaItemId"" IN (
                        SELECT m.""Id"" FROM ""MediaItems"" m
                        WHERE NOT EXISTS (SELECT 1 FROM ""Books"" b WHERE b.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""Videos"" v WHERE v.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""Movies"" mov WHERE mov.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""TvShows"" tv WHERE tv.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""Articles"" a WHERE a.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""PodcastSeries"" ps WHERE ps.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""PodcastEpisodes"" pe WHERE pe.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""YouTubeChannels"" yc WHERE yc.""Id"" = m.""Id"")
                        AND NOT EXISTS (SELECT 1 FROM ""Websites"" w WHERE w.""Id"" = m.""Id"")
                    )";

                // Delete orphaned media items from the base table
                var deleteOrphanedQuery = @"
                    DELETE FROM ""MediaItems"" m
                    WHERE NOT EXISTS (SELECT 1 FROM ""Books"" b WHERE b.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""Videos"" v WHERE v.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""Movies"" mov WHERE mov.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""TvShows"" tv WHERE tv.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""Articles"" a WHERE a.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""PodcastSeries"" ps WHERE ps.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""PodcastEpisodes"" pe WHERE pe.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""YouTubeChannels"" yc WHERE yc.""Id"" = m.""Id"")
                    AND NOT EXISTS (SELECT 1 FROM ""Websites"" w WHERE w.""Id"" = m.""Id"")";

                int joinTableRowsDeleted = 0;
                int orphanedRowsDeleted = 0;

                // Execute the cleanup
                joinTableRowsDeleted = await _context.Database.ExecuteSqlRawAsync(deleteJoinTablesQuery);
                orphanedRowsDeleted = await _context.Database.ExecuteSqlRawAsync(deleteOrphanedQuery);

                _logger.LogInformation($"Fixed orphaned media: deleted {joinTableRowsDeleted} join table entries and {orphanedRowsDeleted} orphaned media items");

                return Ok(new
                {
                    message = "Orphaned media items fixed successfully",
                    joinTableRowsDeleted = joinTableRowsDeleted,
                    orphanedMediaItemsDeleted = orphanedRowsDeleted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing orphaned media");
                return StatusCode(500, new { error = "Failed to fix orphaned media", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-websites
        [HttpPost("cleanup-websites")]
        public async Task<IActionResult> CleanupWebsites()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting websites data cleanup...");

                var websites = await _context.Websites.ToListAsync();
                _context.Websites.RemoveRange(websites);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {websites.Count} websites");

                return Ok(new
                {
                    message = "Websites cleanup completed successfully",
                    deleted = new
                    {
                        websites = websites.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during websites cleanup");
                return StatusCode(500, new { error = "Failed to cleanup websites", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-channels
        [HttpPost("cleanup-channels")]
        public async Task<IActionResult> CleanupChannels()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting YouTube channels data cleanup...");

                var channels = await _context.YouTubeChannels.ToListAsync();
                _context.YouTubeChannels.RemoveRange(channels);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {channels.Count} YouTube channels");

                return Ok(new
                {
                    message = "YouTube channels cleanup completed successfully",
                    deleted = new
                    {
                        channels = channels.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during YouTube channels cleanup");
                return StatusCode(500, new { error = "Failed to cleanup YouTube channels", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-playlists
        [HttpPost("cleanup-playlists")]
        public async Task<IActionResult> CleanupPlaylists()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting YouTube playlists data cleanup...");

                // Delete playlist-video associations first
                var playlistVideos = await _context.Set<YouTubePlaylistVideo>().ToListAsync();
                _context.Set<YouTubePlaylistVideo>().RemoveRange(playlistVideos);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {playlistVideos.Count} playlist-video associations");

                // Delete playlists
                var playlists = await _context.YouTubePlaylists.ToListAsync();
                _context.YouTubePlaylists.RemoveRange(playlists);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {playlists.Count} YouTube playlists");

                return Ok(new
                {
                    message = "YouTube playlists cleanup completed successfully",
                    deleted = new
                    {
                        playlistVideoAssociations = playlistVideos.Count,
                        playlists = playlists.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during YouTube playlists cleanup");
                return StatusCode(500, new { error = "Failed to cleanup YouTube playlists", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-documents
        [HttpPost("cleanup-documents")]
        public async Task<IActionResult> CleanupDocuments()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting documents data cleanup...");

                var documents = await _context.Documents.ToListAsync();
                _context.Documents.RemoveRange(documents);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {documents.Count} documents");

                return Ok(new
                {
                    message = "Documents cleanup completed successfully",
                    deleted = new
                    {
                        documents = documents.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during documents cleanup");
                return StatusCode(500, new { error = "Failed to cleanup documents", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-videos
        [HttpPost("cleanup-videos")]
        public async Task<IActionResult> CleanupVideos()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting videos data cleanup...");

                var videos = await _context.Videos.ToListAsync();
                _context.Videos.RemoveRange(videos);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {videos.Count} videos");

                return Ok(new
                {
                    message = "Videos cleanup completed successfully",
                    deleted = new
                    {
                        videos = videos.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during videos cleanup");
                return StatusCode(500, new { error = "Failed to cleanup videos", details = ex.Message });
            }
        }

        // POST: api/dev/cleanup-all-media
        [HttpPost("cleanup-all-media")]
        public async Task<IActionResult> CleanupAllMedia()
        {
            var envCheck = CheckEnvironment();
            if (envCheck != null) return envCheck;

            try
            {
                _logger.LogInformation("Starting COMPLETE media cleanup (NUCLEAR OPTION)...");

                var counts = new
                {
                    playlistVideos = 0,
                    playlists = 0,
                    videos = 0,
                    channels = 0,
                    episodes = 0,
                    series = 0,
                    books = 0,
                    movies = 0,
                    tvShows = 0,
                    articles = 0,
                    highlights = 0,
                    mixlists = 0
                };

                // Delete playlist-video associations
                var playlistVideos = await _context.Set<YouTubePlaylistVideo>().ToListAsync();
                _context.Set<YouTubePlaylistVideo>().RemoveRange(playlistVideos);
                await _context.SaveChangesAsync();

                // Delete YouTube playlists
                var playlists = await _context.YouTubePlaylists.ToListAsync();
                _context.YouTubePlaylists.RemoveRange(playlists);
                await _context.SaveChangesAsync();

                // Delete highlights
                var highlights = await _context.Highlights.ToListAsync();
                _context.Highlights.RemoveRange(highlights);
                await _context.SaveChangesAsync();

                // Delete podcast episodes
                var episodes = await _context.PodcastEpisodes.ToListAsync();
                _context.PodcastEpisodes.RemoveRange(episodes);
                await _context.SaveChangesAsync();

                // Delete podcast series
                var series = await _context.PodcastSeries.ToListAsync();
                _context.PodcastSeries.RemoveRange(series);
                await _context.SaveChangesAsync();

                // Delete books
                var books = await _context.Books.ToListAsync();
                _context.Books.RemoveRange(books);
                await _context.SaveChangesAsync();

                // Delete movies
                var movies = await _context.Movies.ToListAsync();
                _context.Movies.RemoveRange(movies);
                await _context.SaveChangesAsync();

                // Delete TV shows
                var tvShows = await _context.TvShows.ToListAsync();
                _context.TvShows.RemoveRange(tvShows);
                await _context.SaveChangesAsync();

                // Delete videos
                var videos = await _context.Videos.ToListAsync();
                _context.Videos.RemoveRange(videos);
                await _context.SaveChangesAsync();

                // Delete articles
                var articles = await _context.Articles.ToListAsync();
                _context.Articles.RemoveRange(articles);
                await _context.SaveChangesAsync();

                // Delete YouTube channels
                var channels = await _context.YouTubeChannels.ToListAsync();
                _context.YouTubeChannels.RemoveRange(channels);
                await _context.SaveChangesAsync();

                // Delete mixlists
                var mixlists = await _context.Mixlists.ToListAsync();
                _context.Mixlists.RemoveRange(mixlists);
                await _context.SaveChangesAsync();

                counts = new
                {
                    playlistVideos = playlistVideos.Count,
                    playlists = playlists.Count,
                    videos = videos.Count,
                    channels = channels.Count,
                    episodes = episodes.Count,
                    series = series.Count,
                    books = books.Count,
                    movies = movies.Count,
                    tvShows = tvShows.Count,
                    articles = articles.Count,
                    highlights = highlights.Count,
                    mixlists = mixlists.Count
                };

                _logger.LogInformation("COMPLETE media cleanup finished");

                return Ok(new
                {
                    message = "COMPLETE media cleanup completed successfully (NUCLEAR OPTION)",
                    deleted = counts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during complete media cleanup");
                return StatusCode(500, new { error = "Failed to cleanup all media", details = ex.Message });
            }
        }

    }
}
