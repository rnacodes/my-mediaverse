# Testing Guide for ProjectLoopbreaker

This document describes the testing infrastructure, patterns, and conventions used in ProjectLoopbreaker.

## Quick Start

### Run All Tests

```powershell
# From the root directory
.\run-all-tests.ps1         # Runs backend + frontend tests with logging
```

### Run Backend Tests Only

```powershell
# Unit tests
dotnet test tests/ProjectLoopbreaker.UnitTests/ProjectLoopbreaker.UnitTests.csproj

# Integration tests
dotnet test tests/ProjectLoopbreaker.IntegrationTests/ProjectLoopbreaker.IntegrationTests.csproj

# All backend tests with coverage
.\run-backend-tests.ps1
```

### Run Frontend Tests Only

```powershell
# From frontend/ directory
npm test                    # Watch mode
npm run test:run            # Single run
npm run test:coverage       # With coverage report
```

---

## Test Project Structure

```
ProjectLoopbreaker/
├── tests/
│   ├── ProjectLoopbreaker.UnitTests/
│   │   ├── Application/          # Service layer tests
│   │   ├── Domain/               # Entity tests
│   │   ├── Infrastructure/       # API client tests
│   │   ├── TestHelpers/          # InMemoryDbTestBase.cs
│   │   └── TestData/             # TestDataFactory.cs
│   └── ProjectLoopbreaker.IntegrationTests/
│       ├── Controllers/          # HTTP endpoint tests
│       └── WebApplicationFactory.cs
├── frontend/src/
│   ├── components/__tests__/     # Component tests
│   ├── contexts/__tests__/       # Context tests (if added)
│   └── test-setup.js             # Global test configuration
└── logs/                         # Test output logs (timestamped)
```

---

## Backend Testing

### Test Framework & Libraries

- **xUnit** - Test framework
- **Moq** - Mocking library
- **FluentAssertions** - Readable assertions
- **AutoFixture** - Test data generation
- **Microsoft.EntityFrameworkCore.InMemory** - In-memory database
- **Testcontainers.PostgreSql** - Docker-based PostgreSQL for integration tests

### Unit Test Pattern

All service tests should inherit from `InMemoryDbTestBase`:

```csharp
using FluentAssertions;
using Moq;
using ProjectLoopbreaker.UnitTests.TestData;
using ProjectLoopbreaker.UnitTests.TestHelpers;

namespace ProjectLoopbreaker.UnitTests.Application
{
    public class MyServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<ILogger<MyService>> _mockLogger;
        private readonly MyService _service;

        public MyServiceTests()
        {
            _mockLogger = new Mock<ILogger<MyService>>();
            _service = new MyService(Context, _mockLogger.Object);
        }

        [Fact]
        public async Task MethodName_Scenario_ExpectedResult()
        {
            // Arrange
            var entity = TestDataFactory.CreateEntity();
            Context.Entities.Add(entity);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.MethodAsync();

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Expected Title");
        }

        [Theory]
        [InlineData("value1", ExpectedResult1)]
        [InlineData("value2", ExpectedResult2)]
        public async Task MethodName_WithVariousInputs_ReturnsExpected(string input, int expected)
        {
            // Parameterized test pattern
        }
    }
}
```

### Test Data Factory

Use `TestDataFactory` for creating test entities and DTOs:

```csharp
// Entities
var book = TestDataFactory.CreateBook("Title", "Author");
var books = TestDataFactory.CreateBooks(5);
var podcast = TestDataFactory.CreatePodcastSeries("Podcast Name");
var movie = TestDataFactory.CreateMovie("Movie Title");
var document = TestDataFactory.CreateDocument("Document Title");

// DTOs
var bookDto = TestDataFactory.CreateBookDto("Title", "Author");
var mixlistDto = TestDataFactory.CreateMixlistDto("Mixlist Name");
```

### Integration Test Pattern

Integration tests use `WebApplicationFactory` to test full HTTP request/response cycles:

```csharp
public class MyControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public MyControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/myresource");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<MyDto>>(content, _jsonOptions);
        Assert.NotNull(items);
    }
}
```

### API Client Test Pattern

For testing HTTP clients, mock the HttpMessageHandler:

```csharp
public class MyApiClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly MyApiClient _client;

    public MyApiClientTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };
        _client = new MyApiClient(_httpClient);
    }

    private void SetupHttpResponse(HttpStatusCode status, string content)
    {
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }
}
```

---

## Frontend Testing

### Test Framework & Libraries

- **Vitest** - Test runner (compatible with Jest API)
- **React Testing Library** - Component testing
- **@testing-library/jest-dom** - DOM matchers
- **@testing-library/user-event** - User interaction simulation
- **jsdom** - DOM implementation

### Configuration

**vitest.config.js:**
```javascript
export default defineConfig({
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test-setup.js'],
    globals: true,  // describe, it, expect available globally
    css: true,
    env: {
      VITE_API_URL: 'http://localhost:5033/api'
    }
  }
});
```

### Component Test Pattern

```javascript
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi } from 'vitest';
import MyComponent from '../MyComponent';
import * as apiService from '../../api';

// Mock API module
vi.mock('../../api');

// Helper to render with router context
const renderWithRouter = (component) => {
  return render(<BrowserRouter>{component}</BrowserRouter>);
};

describe('MyComponent', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should render loading state initially', () => {
    apiService.getData.mockResolvedValue({ data: [] });
    renderWithRouter(<MyComponent />);

    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  it('should display data when loaded', async () => {
    const mockData = [{ id: 1, title: 'Test Item' }];
    apiService.getData.mockResolvedValue({ data: mockData });

    renderWithRouter(<MyComponent />);

    await waitFor(() => {
      expect(screen.getByText('Test Item')).toBeInTheDocument();
    });
  });

  it('should handle API errors gracefully', async () => {
    apiService.getData.mockRejectedValue(new Error('API Error'));

    renderWithRouter(<MyComponent />);

    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });

  it('should call API with correct parameters on submit', async () => {
    apiService.createItem.mockResolvedValue({ data: { id: 1 } });

    renderWithRouter(<MyComponent />);

    fireEvent.change(screen.getByLabelText('Title'), {
      target: { value: 'New Item' }
    });
    fireEvent.click(screen.getByRole('button', { name: /submit/i }));

    await waitFor(() => {
      expect(apiService.createItem).toHaveBeenCalledWith({
        title: 'New Item'
      });
    });
  });
});
```

### Mock Patterns

**Mocking React Router:**
```javascript
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => vi.fn(),
    useParams: () => ({ id: 'test-id' }),
  };
});
```

**Mocking API Service:**
```javascript
vi.mock('../../api');

// In test:
apiService.getAllItems.mockResolvedValue({ data: mockItems });
apiService.createItem.mockRejectedValue(new Error('Validation failed'));
```

**Timer Mocking:**
```javascript
beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.runAllTimers();
  vi.useRealTimers();
});

it('should debounce search', async () => {
  renderWithRouter(<SearchComponent />);

  fireEvent.change(screen.getByRole('textbox'), { target: { value: 'test' } });

  expect(apiService.search).not.toHaveBeenCalled();

  vi.advanceTimersByTime(500);

  expect(apiService.search).toHaveBeenCalledWith('test');
});
```

---

## Naming Conventions

### Test Files

- Backend: `{ClassName}Tests.cs` (e.g., `BookServiceTests.cs`)
- Frontend: `{ComponentName}.test.jsx` (e.g., `AllMedia.test.jsx`)

### Test Method Names

Backend (follows pattern `MethodName_Scenario_ExpectedResult`):
```csharp
GetAllBooksAsync_ShouldReturnAllBooks()
GetBookByIdAsync_WhenBookDoesNotExist_ReturnsNull()
CreateBookAsync_WithValidData_ReturnsCreatedBook()
ValidateRefreshToken_WhenTokenExpired_ReturnsFalse()
```

Frontend (uses descriptive strings):
```javascript
it('should render the list of items')
it('should display loading spinner while fetching')
it('should show error message when API fails')
it('should navigate to detail page on item click')
```

---

## Test Coverage Guidelines

### What to Test

**Backend:**
- Service CRUD operations
- Validation logic
- Error handling
- External API client responses
- Token generation/validation (auth)
- Edge cases (null inputs, empty collections)

**Frontend:**
- Component rendering
- User interactions (clicks, form inputs)
- API call verification
- Loading/error/empty states
- Navigation
- Form validation

### What NOT to Test

- Simple getters/setters without logic
- Framework code (EF Core, React Router internals)
- Third-party libraries
- Private methods (test through public API)

---

## Common Assertions

### Backend (FluentAssertions)

```csharp
result.Should().NotBeNull();
result.Should().BeNull();
result.Should().HaveCount(5);
result.Should().BeEmpty();
result.Should().Contain(item);
result.Title.Should().Be("Expected");
result.Should().BeEquivalentTo(expected);
result.Should().Throw<InvalidOperationException>();
```

### Frontend (Testing Library / Jest DOM)

```javascript
expect(element).toBeInTheDocument();
expect(element).not.toBeInTheDocument();
expect(element).toHaveTextContent('text');
expect(element).toHaveAttribute('href', '/path');
expect(element).toBeDisabled();
expect(element).toHaveClass('active');
expect(mockFn).toHaveBeenCalled();
expect(mockFn).toHaveBeenCalledWith(arg);
expect(mockFn).toHaveBeenCalledTimes(2);
```

---

## Test Output & Logs

Test logs are saved to the `logs/` directory with timestamps:
- `backend-tests-YYYYMMDD-HHMMSS.log`
- `frontend-tests-YYYYMMDD-HHMMSS.log`

---

## Troubleshooting

### Tests Fail with Database Errors
- Ensure you're running against in-memory database (unit tests) or test containers (integration tests)
- Check that `InMemoryDbTestBase` is properly inherited

### Frontend Tests Hang
- Check for unhandled promises or async operations
- Ensure timers are cleaned up with `vi.useRealTimers()`
- Look for infinite re-renders in components

### Flaky Tests
- Use `waitFor` for async assertions
- Avoid testing implementation details
- Mock all external dependencies consistently
