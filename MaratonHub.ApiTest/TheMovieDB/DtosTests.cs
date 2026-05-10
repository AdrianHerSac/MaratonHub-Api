using MaratonHub.Api.TheMovieDB.Dtos;

namespace MaratonHub.ApiTest.TheMovieDB;

public class MovieDtoTests
{
    [Test]
    public void MovieDto_ShouldInitializeWithDefaultValues()
    {
        var movie = new MovieDto();

        Assert.That(movie.Id, Is.EqualTo(0));
        Assert.That(movie.Title, Is.EqualTo(string.Empty));
        Assert.That(movie.Overview, Is.EqualTo(string.Empty));
        Assert.That(movie.PosterPath, Is.Null);
        Assert.That(movie.BackdropPath, Is.Null);
        Assert.That(movie.ReleaseDate, Is.Null);
        Assert.That(movie.VoteAverage, Is.EqualTo(0));
        Assert.That(movie.VoteCount, Is.EqualTo(0));
        Assert.That(movie.OriginalLanguage, Is.Null);
        Assert.That(movie.Genres, Is.Not.Null);
        Assert.That(movie.Genres.Count, Is.EqualTo(0));
    }

    [Test]
    public void MovieDto_ShouldAllowSettingAllProperties()
    {
        var movie = new MovieDto
        {
            Id = 123,
            Title = "Test Movie",
            Overview = "Test Overview",
            PosterPath = "/poster.jpg",
            BackdropPath = "/backdrop.jpg",
            ReleaseDate = new DateTime(2024, 1, 1),
            VoteAverage = 8.5,
            VoteCount = 1000,
            OriginalLanguage = "es",
            Genres = new List<GenreDto> { new() { Id = 1, Name = "Action" } }
        };

        Assert.That(movie.Id, Is.EqualTo(123));
        Assert.That(movie.Title, Is.EqualTo("Test Movie"));
        Assert.That(movie.Overview, Is.EqualTo("Test Overview"));
        Assert.That(movie.PosterPath, Is.EqualTo("/poster.jpg"));
        Assert.That(movie.BackdropPath, Is.EqualTo("/backdrop.jpg"));
        Assert.That(movie.VoteAverage, Is.EqualTo(8.5));
        Assert.That(movie.VoteCount, Is.EqualTo(1000));
        Assert.That(movie.OriginalLanguage, Is.EqualTo("es"));
        Assert.That(movie.Genres.Count, Is.EqualTo(1));
    }

    [Test]
    public void MovieDto_ReleaseDate_ShouldBeNullable()
    {
        var movie = new MovieDto { ReleaseDate = null };
        Assert.That(movie.ReleaseDate, Is.Null);

        movie.ReleaseDate = new DateTime(2024, 6, 15);
        Assert.That(movie.ReleaseDate, Is.EqualTo(new DateTime(2024, 6, 15)));
    }

    [Test]
    public void MovieDto_MultipleGenres_ShouldWorkCorrectly()
    {
        var movie = new MovieDto
        {
            Genres = new List<GenreDto>
            {
                new() { Id = 28, Name = "Action" },
                new() { Id = 12, Name = "Adventure" },
                new() { Id = 878, Name = "Science Fiction" }
            }
        };

        Assert.That(movie.Genres.Count, Is.EqualTo(3));
        Assert.That(movie.Genres[0].Name, Is.EqualTo("Action"));
        Assert.That(movie.Genres[1].Name, Is.EqualTo("Adventure"));
        Assert.That(movie.Genres[2].Name, Is.EqualTo("Science Fiction"));
    }

    [Test]
    public void MovieDto_Serialization_ShouldWorkCorrectly()
    {
        var movie = new MovieDto
        {
            Id = 100,
            Title = "Test Film",
            Overview = "Overview",
            VoteAverage = 7.8,
            Genres = new List<GenreDto> { new() { Id = 1, Name = "Drama" } }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(movie);
        Assert.That(json, Is.Not.Null.And.Not.Empty);
        Assert.That(json, Does.Contain("Test Film"));
        Assert.That(json, Does.Contain("Drama"));

        var deserialized = System.Text.Json.JsonSerializer.Deserialize<MovieDto>(json);
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Title, Is.EqualTo("Test Film"));
        Assert.That(deserialized.VoteAverage, Is.EqualTo(7.8));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// TvShowDto Tests
// ══════════════════════════════════════════════════════════════════════════════

public class TvShowDtoTests
{
    [Test]
    public void TvShowDto_ShouldInitializeWithDefaultValues()
    {
        var tvShow = new TvShowDto();

        Assert.That(tvShow.Id, Is.EqualTo(0));
        Assert.That(tvShow.Name, Is.EqualTo(string.Empty));
        Assert.That(tvShow.Overview, Is.EqualTo(string.Empty));
        Assert.That(tvShow.PosterPath, Is.Null);
        Assert.That(tvShow.BackdropPath, Is.Null);
        Assert.That(tvShow.FirstAirDate, Is.Null);
        Assert.That(tvShow.VoteAverage, Is.EqualTo(0));
        Assert.That(tvShow.VoteCount, Is.EqualTo(0));
        Assert.That(tvShow.OriginalLanguage, Is.Null);
        Assert.That(tvShow.NumberOfSeasons, Is.Null);
        Assert.That(tvShow.NumberOfEpisodes, Is.Null);
        Assert.That(tvShow.Status, Is.Null);
        Assert.That(tvShow.Genres, Is.Not.Null);
    }

    [Test]
    public void TvShowDto_ShouldAllowSettingAllProperties()
    {
        var tvShow = new TvShowDto
        {
            Id = 456,
            Name = "Test TV Show",
            Overview = "Test Overview",
            PosterPath = "/poster.jpg",
            BackdropPath = "/backdrop.jpg",
            FirstAirDate = new DateTime(2020, 3, 15),
            VoteAverage = 9.1,
            VoteCount = 5000,
            OriginalLanguage = "en",
            NumberOfSeasons = 5,
            NumberOfEpisodes = 50,
            Status = "Running",
            Genres = new List<GenreDto> { new() { Id = 18, Name = "Drama" } }
        };

        Assert.That(tvShow.Id, Is.EqualTo(456));
        Assert.That(tvShow.Name, Is.EqualTo("Test TV Show"));
        Assert.That(tvShow.NumberOfSeasons, Is.EqualTo(5));
        Assert.That(tvShow.NumberOfEpisodes, Is.EqualTo(50));
        Assert.That(tvShow.Status, Is.EqualTo("Running"));
        Assert.That(tvShow.Genres.Count, Is.EqualTo(1));
    }

    [Test]
    public void TvShowDto_FirstAirDate_ShouldBeNullable()
    {
        var tvShow = new TvShowDto { FirstAirDate = null };
        Assert.That(tvShow.FirstAirDate, Is.Null);

        tvShow.FirstAirDate = new DateTime(2024, 1, 1);
        Assert.That(tvShow.FirstAirDate, Is.EqualTo(new DateTime(2024, 1, 1)));
    }

    [Test]
    public void TvShowDto_Serialization_ShouldPreserveAllFields()
    {
        var show = new TvShowDto
        {
            Id = 1,
            Name = "Test",
            NumberOfSeasons = 3,
            NumberOfEpisodes = 24,
            Status = "Ended"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(show);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TvShowDto>(json);

        Assert.That(deserialized!.NumberOfSeasons, Is.EqualTo(3));
        Assert.That(deserialized.NumberOfEpisodes, Is.EqualTo(24));
        Assert.That(deserialized.Status, Is.EqualTo("Ended"));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// PersonDto Tests
// ══════════════════════════════════════════════════════════════════════════════

public class PersonDtoTests
{
    [Test]
    public void PersonDto_ShouldInitializeWithDefaultValues()
    {
        var person = new PersonDto();

        Assert.That(person.Id, Is.EqualTo(0));
        Assert.That(person.Name, Is.EqualTo(string.Empty));
        Assert.That(person.ProfilePath, Is.Null);
        Assert.That(person.Popularity, Is.EqualTo(0));
        Assert.That(person.KnownForDepartment, Is.Null);
        Assert.That(person.Biography, Is.Null);
        Assert.That(person.Birthday, Is.Null);
        Assert.That(person.PlaceOfBirth, Is.Null);
    }

    [Test]
    public void PersonDto_ShouldAllowSettingAllProperties()
    {
        var person = new PersonDto
        {
            Id = 789,
            Name = "Test Actor",
            ProfilePath = "/profile.jpg",
            Popularity = 10.5,
            KnownForDepartment = "Acting",
            Biography = "Test biography about this person.",
            Birthday = new DateTime(1990, 1, 1),
            PlaceOfBirth = "Madrid, Spain"
        };

        Assert.That(person.Id, Is.EqualTo(789));
        Assert.That(person.Name, Is.EqualTo("Test Actor"));
        Assert.That(person.ProfilePath, Is.EqualTo("/profile.jpg"));
        Assert.That(person.KnownForDepartment, Is.EqualTo("Acting"));
        Assert.That(person.Biography, Is.EqualTo("Test biography about this person."));
        Assert.That(person.Birthday, Is.EqualTo(new DateTime(1990, 1, 1)));
        Assert.That(person.PlaceOfBirth, Is.EqualTo("Madrid, Spain"));
    }

    [Test]
    public void PersonDto_Birthday_ShouldBeNullable()
    {
        var person = new PersonDto { Birthday = null };
        Assert.That(person.Birthday, Is.Null);

        person.Birthday = new DateTime(1985, 5, 20);
        Assert.That(person.Birthday, Is.EqualTo(new DateTime(1985, 5, 20)));
    }

    [Test]
    public void PersonDto_Serialization_ShouldWorkCorrectly()
    {
        var person = new PersonDto { Id = 1, Name = "Actor", Popularity = 50.0 };

        var json = System.Text.Json.JsonSerializer.Serialize(person);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<PersonDto>(json);

        Assert.That(deserialized!.Name, Is.EqualTo("Actor"));
        Assert.That(deserialized.Popularity, Is.EqualTo(50.0));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GenreDto Tests
// ══════════════════════════════════════════════════════════════════════════════

public class GenreDtoTests
{
    [Test]
    public void GenreDto_ShouldInitializeWithDefaultValues()
    {
        var genre = new GenreDto();

        Assert.That(genre.Id, Is.EqualTo(0));
        Assert.That(genre.Name, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GenreDto_ShouldAllowSettingProperties()
    {
        var genre = new GenreDto { Id = 1, Name = "Comedy" };

        Assert.That(genre.Id, Is.EqualTo(1));
        Assert.That(genre.Name, Is.EqualTo("Comedy"));
    }

    [Test]
    public void GenreDto_Serialization_ShouldWorkCorrectly()
    {
        var genre = new GenreDto { Id = 28, Name = "Action" };

        var json = System.Text.Json.JsonSerializer.Serialize(genre);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<GenreDto>(json);

        Assert.That(deserialized!.Id, Is.EqualTo(28));
        Assert.That(deserialized.Name, Is.EqualTo("Action"));
    }
}