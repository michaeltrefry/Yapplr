# Yapplr.Api.Tests

This is the unit test project for the Yapplr API. It contains all unit tests for testing the API functionality in isolation from the main application.

## Project Structure

```
Yapplr.Api.Tests/
├── Yapplr.Api.Tests.csproj    # Test project configuration
├── MentionParserTests.cs      # Tests for mention parsing functionality
└── README.md                  # This file
```

## Technologies Used

- **xUnit** - Testing framework
- **Moq** - Mocking framework for dependencies
- **Entity Framework Core InMemory** - In-memory database for testing
- **.NET 9** - Target framework

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run tests from the test project directory
```bash
cd Yapplr.Api.Tests
dotnet test
```

### Run tests with detailed output
```bash
dotnet test --verbosity detailed
```

### Run specific test class
```bash
dotnet test --filter "ClassName=MentionParserTests"
```

## Test Organization

Tests are organized by the functionality they test:

- **MentionParserTests.cs** - Tests for the `MentionParser` utility class
  - Tests mention extraction from text
  - Tests mention validation
  - Tests mention position detection
  - Tests mention link replacement

## Adding New Tests

When adding new tests:

1. Create test files following the naming convention: `[ClassName]Tests.cs`
2. Use the `Yapplr.Api.Tests` namespace
3. Follow the Arrange-Act-Assert pattern
4. Use descriptive test method names that explain what is being tested
5. Add appropriate test categories using `[Fact]` or `[Theory]` attributes

## Test Dependencies

The test project references:
- **Yapplr.Api** - The main API project being tested
- **Microsoft.EntityFrameworkCore.InMemory** - For database testing
- **Moq** - For mocking dependencies
- **xUnit** - Testing framework and test runner

## Best Practices

- Keep tests isolated and independent
- Use in-memory databases for data access tests
- Mock external dependencies
- Test both happy path and edge cases
- Use meaningful test data
- Keep tests fast and reliable
