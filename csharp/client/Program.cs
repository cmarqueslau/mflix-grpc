/*
    Data de criação: 03/11/2026
    Aluna: Caroline Marques Lau
    Arquivo do cliente que realiza operações no banco
*/

using DotNetEnv;
using Mflix;
using Serilog;
using Grpc.Core;
using Grpc.Net.Client;

public class Client
{
    public static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .CreateLogger();
        try
        {
            Env.TraversePath().Load();

            using GrpcChannel channel = CreateChannel();
            var client = new MovieService.MovieServiceClient(channel);

            bool running = true;

            while (running)
            {
                ShowMenu();
                string? option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        Log.Information("Opção selecionada: criar filme.");
                        await CreateMovie(client);
                        break;

                    case "2":
                        Log.Information("Opção selecionada: consultar filme.");
                        await GetMovie(client);
                        break;

                    case "3":
                        Log.Information("Opção selecionada: atualizar filme.");
                        await UpdateMovie(client);
                        break;

                    case "4":
                        Log.Information("Opção selecionada: excluir filme.");
                        await DeleteMovie(client);
                        break;

                    case "5":
                        Log.Information("Opção selecionada: pesquisar por ator.");
                        await ListMoviesByActor(client);
                        break;

                    case "6":
                        Log.Information("Opção selecionada: pesquisar por gênero.");
                        await ListMoviesByGenre(client);
                        break;

                    case "0":
                        running = false;
                        Log.Information("Encerrando cliente...");
                        break;

                    default:
                        Log.Information("Opção inválida. Tente novamente.");
                        break;
                }
            }

        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
    private static void ShowMenu() // Comandos disponíveis
    {
        Console.WriteLine();
        Console.WriteLine("======= MFlix Client =======");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("1.");
        Console.ResetColor();
        Console.WriteLine(" Criar filme");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("2.");
        Console.ResetColor();
        Console.WriteLine(" Consultar filme por ID");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("3.");
        Console.ResetColor();
        Console.WriteLine(" Atualizar filme");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("4.");
        Console.ResetColor();
        Console.WriteLine(" Excluir filme");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("5.");
        Console.ResetColor();
        Console.WriteLine(" Listar filmes por ator");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("6.");
        Console.ResetColor();
        Console.WriteLine(" Listar filmes por gênero");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("0.");
        Console.ResetColor();
        Console.WriteLine(" Sair");

        Console.WriteLine("============================");
        Console.WriteLine();
        Console.Write("Escolha uma opção: ");
    }

    private static GrpcChannel CreateChannel() // Realizar a conexão com o servidor
    {
        string serverAddress =
            Environment.GetEnvironmentVariable("SERVER_ADDR")
            ?? throw new InvalidOperationException(
                "SERVER_ADDR não configurada no .env."
            );

        return GrpcChannel.ForAddress($"http://{serverAddress}");
    }

    private static async Task CreateMovie(MovieService.MovieServiceClient client) // Criar um novo registro de filme
    {
        var movie = new Movie();

        Log.Information("Iniciando cadastro de filme.");

        // Solicita o título até que seja válido.
        while (true)
        {
            Console.Write("Título: ");
            movie.Title = Console.ReadLine() ?? "";

            if (!string.IsNullOrWhiteSpace(movie.Title))
                break;

            Log.Warning("O título não pode ser vazio. Tente novamente.");
        }

        // Solicita o ano até que seja válido.
        while (true)
        {
            Console.Write("Ano: ");
            string? yearInput = Console.ReadLine();

            if (int.TryParse(yearInput, out int year))
            {
                movie.Year = year;
                break;
            }

            Log.Warning("Ano inválido. Digite um número inteiro.");
        }

        Console.Write("Sinopse: ");
        movie.Plot = Console.ReadLine() ?? "";

        Console.Write("Duração (minutos): ");
        string? runtimeInput = Console.ReadLine();

        if (int.TryParse(runtimeInput, out int runtime))
            movie.Runtime = runtime;

        Console.Write("Gêneros (separados por vírgula): ");
        movie.Genres.AddRange(ReadList());

        Console.Write("Elenco (separado por vírgula): ");
        movie.Cast.AddRange(ReadList());

        Console.Write("Número de comentários: ");
        string? commentsInput = Console.ReadLine();

        if (int.TryParse(commentsInput, out int comments))
            movie.NumMflixComments = comments;

        Console.Write("Sinopse completa: ");
        movie.Fullplot = Console.ReadLine() ?? "";

        Console.Write("Países (separados por vírgula): ");
        movie.Countries.AddRange(ReadList());

        Console.Write("Data de lançamento: ");
        string? releasedInput = Console.ReadLine();

        if (DateTime.TryParse(releasedInput, out DateTime released))
        {
            movie.Released = released
                .ToUniversalTime()
                .ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        Console.Write("Diretores (separados por vírgula): ");
        movie.Directors.AddRange(ReadList());

        Console.Write("Classificação indicativa: ");
        movie.Rated = Console.ReadLine() ?? "";

        // Preenchida automaticamente com a data e hora atuais em UTC.
        movie.Lastupdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

        Console.WriteLine($"Última atualização: {movie.Lastupdated}");

        Console.Write("Tipo: ");
        movie.Type = Console.ReadLine() ?? "";

        Console.WriteLine();


        try
        {
            Movie movieCreated = await client.CreateMovieAsync(new CreateMovieRequest { Movie = movie });
            Log.Information(
                "Filme criado com sucesso. ID: {MovieId}, Título: {Title}",
                movieCreated.Id,
                movieCreated.Title
            );
        }
        catch (RpcException ex)
        {
            Log.Error(
                "Erro ao criar filme. Status: {Status}, Mensagem: {Error}",
                ex.Status.StatusCode,
                ex.Status.Detail
            );
        }
    }

    private static List<string> ReadList() // Função auxiliar para leitura de listas
    {
        return [.. (Console.ReadLine() ?? "")
            .Split(
                ',',
                StringSplitOptions.TrimEntries |
                StringSplitOptions.RemoveEmptyEntries
            )];
    }

    static void PrintAttribute(string name, string? value) // Estilização de atributos
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"{name}: ");
        Console.ResetColor();

        Console.WriteLine(value);
    }

    private static async Task GetMovie(MovieService.MovieServiceClient client) // Obter um filme do banco
    {
        Console.Write("Digite o ID do filme: ");
        string? id = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(id))
        {
            Console.WriteLine("O ID não pode ser vazio.");
            Log.Warning("Consulta de filme cancelada: ID vazio.");
            return;
        }

        try
        {
            Movie movie = await client.GetMovieAsync(new GetMovieRequest { Id = id });

            Log.Information(
                "Filme encontrado: {Id} - {Title}",
                movie.Id,
                movie.Title
            );

            Console.WriteLine("\n======== Filme encontrado ========");

            PrintAttribute("ID", movie.Id);
            PrintAttribute("Título", movie.Title);
            PrintAttribute("Ano", movie.Year != 0 ? movie.Year.ToString() : null);
            PrintAttribute("Sinopse", movie.Plot);
            PrintAttribute("Duração", movie.Runtime != 0
                ? $"{movie.Runtime} minutos"
                : null);
            PrintAttribute("Gêneros", movie.Genres.Count > 0
                ? string.Join(", ", movie.Genres)
                : null);
            PrintAttribute("Elenco", movie.Cast.Count > 0
                ? string.Join(", ", movie.Cast)
                : null);
            PrintAttribute("Número de comentários", movie.NumMflixComments != 0
                ? movie.NumMflixComments.ToString()
                : null);
            PrintAttribute("Sinopse completa", movie.Fullplot);
            PrintAttribute("Países", movie.Countries.Count > 0
                ? string.Join(", ", movie.Countries)
                : null);
            PrintAttribute("Lançamento", movie.Released);
            PrintAttribute("Diretores", movie.Directors.Count > 0
                ? string.Join(", ", movie.Directors)
                : null);
            PrintAttribute("Classificação", movie.Rated);
            PrintAttribute("Última atualização", movie.Lastupdated);
            PrintAttribute("Tipo", movie.Type);
            Console.WriteLine();
        }
        catch (RpcException ex)
        {
            Log.Warning(
                "Consulta do filme {Id} falhou. Status: {Status}. Motivo: {Error}",
                id,
                ex.Status,
                ex.Status.Detail
            );

            Console.WriteLine($"Erro: {ex.Status.Detail}");
        }
    }

    private static async Task UpdateMovie(MovieService.MovieServiceClient client) // Atualizar registros de um filme
    {
        Console.Write("ID do filme: ");
        string? id = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(id))
            return;

        Movie m;

        Log.Information("Consultando filme {Id} antes da atualização.", id);

        try
        {
            m = await client.GetMovieAsync(new GetMovieRequest { Id = id });

        }
        catch (RpcException ex)
        {
            Log.Warning(
                "Não foi possível consultar o filme {Id}: {Error}",
                id,
                ex.Status.Detail
            );

            Console.WriteLine($"Erro: {ex.Status.Detail}");
            return;
        }

        Log.Information("Filme {Id} carregado para edição.", id);

        Console.Write($"Título ({m.Title}): ");
        string? input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input)) m.Title = input;

        Console.Write($"Ano ({m.Year}): ");
        input = Console.ReadLine();
        if (int.TryParse(input, out int year)) m.Year = year;

        Console.Write($"Sinopse ({m.Plot}): ");
        input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input)) m.Plot = input;

        Console.Write($"Duração ({m.Runtime}): ");
        input = Console.ReadLine();
        if (int.TryParse(input, out int runtime)) m.Runtime = runtime;

        Console.Write($"Gêneros ({string.Join(", ", m.Genres)}): ");
        input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
        {
            m.Genres.Clear();
            m.Genres.AddRange(input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }

        Console.Write($"Elenco ({string.Join(", ", m.Cast)}): ");
        input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
        {
            m.Cast.Clear();
            m.Cast.AddRange(input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }

        Console.Write($"Número de comentários ({m.NumMflixComments}): ");
        input = Console.ReadLine();
        if (int.TryParse(input, out int comments)) m.NumMflixComments = comments;

        Console.Write($"Sinopse completa ({m.Fullplot}): ");
        input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input)) m.Fullplot = input;

        Console.Write($"Países ({string.Join(", ", m.Countries)}): ");
        input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
        {
            m.Countries.Clear();
            m.Countries.AddRange(input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }

        Console.Write($"Lançamento ({m.Released}): ");
        input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input)) m.Released = input;

        Console.Write($"Diretores ({string.Join(", ", m.Directors)}): ");
        input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
        {
            m.Directors.Clear();
            m.Directors.AddRange(input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }

        Console.Write($"Classificação ({m.Rated}): ");
        input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input)) m.Rated = input;

        m.Lastupdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

        Console.Write($"Tipo ({m.Type}): ");
        input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input)) m.Type = input;

        try
        {
            Movie movie = await client.UpdateMovieAsync(new UpdateMovieRequest { Movie = m });
            Log.Information("Filme {Id} atualizado com sucesso: {Title}", id, movie.Title);
        }
        catch (RpcException ex)
        {
            Log.Warning("Falha ao atualizar filme {Id}. Status: {Status}. Motivo: {Error}", id, ex.Status.StatusCode, ex.Status.Detail);
        }
    }

    private static async Task DeleteMovie(MovieService.MovieServiceClient client) // Deletar um filme existente
    {
        Console.Write("Digite o ID do filme que deseja excluir: ");
        string? id = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(id))
        {
            Log.Warning("Exclusão de filme cancelada: ID vazio.");
            return;
        }

        Console.Write($"Tem certeza de que deseja excluir o filme {id}? (s/n): ");
        string? confirmation = Console.ReadLine();

        if (!string.Equals(confirmation, "s", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Exclusão do filme {Id} cancelada pelo utilizador.", id);
            return;
        }

        try
        {
            await client.DeleteMovieAsync(new DeleteMovieRequest { Id = id });

            Log.Information("Filme {Id} excluído com sucesso.", id);
        }
        catch (RpcException ex)
        {
            Log.Warning(
                "Falha ao excluir filme {Id}. Status: {Status}. Motivo: {Error}",
                id,
                ex.StatusCode,
                ex.Status.Detail
            );
        }
    }
    private static async Task ListMoviesByActor(MovieService.MovieServiceClient client) // Listar filmes pelo elenco
    {
        Console.Write("Digite o nome do ator: ");
        string? actor = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(actor))
        {
            Log.Warning("Pesquisa por ator cancelada: nome vazio.");
            return;
        }

        Log.Information("Pesquisando filmes do ator {Actor}", actor);

        try
        {
            using var call = client.ListByActor(new ListByActorRequest { Actor = actor });

            int count = 0;

            await foreach (Movie movie in call.ResponseStream.ReadAllAsync())
            {
                if (count == 0)
                    Console.WriteLine($"\n======== Filmes com {actor} ========");

                count++;

                Console.WriteLine();
                Console.WriteLine();

                PrintAttribute("ID", movie.Id);
                PrintAttribute("Título", movie.Title);
                PrintAttribute("Ano", movie.Year != 0 ? movie.Year.ToString() : null);
                PrintAttribute(
                    "Gêneros",
                    movie.Genres.Count > 0
                        ? string.Join(", ", movie.Genres)
                        : null
                );
            }

            Log.Information(
                "Pesquisa por ator {Actor} concluída. Filmes encontrados: {Count}",
                actor,
                count
            );

            if (count == 0)
            {
                Log.Information("Nenhum filme encontrado para esse ator.");
                return;
            }

            Console.WriteLine($"\nTotal de filmes encontrados: {count}");
            Console.WriteLine();
        }
        catch (RpcException ex)
        {
            Log.Warning(
                "Falha na pesquisa por ator {Actor}. Status: {Status}. Motivo: {Error}",
                actor,
                ex.StatusCode,
                ex.Status.Detail
            );
        }
    }
    private static async Task ListMoviesByGenre(MovieService.MovieServiceClient client) // Listar filmes pelo gênero
    {
        Console.Write("Digite o gênero dos filmes: ");
        string? genre = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(genre))
        {
            Log.Warning("Pesquisa por gênero cancelada: gênero vazio.");
            Console.WriteLine("O gênero não pode ser vazio.");
            return;
        }

        try
        {
            using var call = client.ListByGenre(new ListByGenreRequest { Genre = genre });

            int count = 0;

            await foreach (Movie movie in call.ResponseStream.ReadAllAsync())
            {
                if (count == 0)
                    Console.WriteLine($"\n======== Filmes do gênero {genre} ========");

                count++;

                Console.WriteLine();
                Console.WriteLine();

                PrintAttribute("ID", movie.Id);
                PrintAttribute("Título", movie.Title);
                PrintAttribute("Ano", movie.Year != 0 ? movie.Year.ToString() : null);
                PrintAttribute(
                    "Gêneros",
                    movie.Genres.Count > 0
                        ? string.Join(", ", movie.Genres)
                        : null
                );
            }

            Log.Information(
                "Pesquisa pelo gênero {Genre} concluída. Total de filmes: {Count}",
                genre,
                count
            );

            if (count == 0)
            {
                Log.Information(
                    "Nenhum filme encontrado para o gênero {Genre}",
                    genre
                );
                return;
            }

            Console.WriteLine($"\nTotal de filmes encontrados: {count}");
            Console.WriteLine();
        }
        catch (RpcException ex)
        {
            Log.Warning(
                "Pesquisa por gênero {Genre} falhou. Status: {Status}. Motivo: {Error}",
                genre,
                ex.StatusCode,
                ex.Status.Detail
            );

            Console.WriteLine($"Erro ({ex.StatusCode}): {ex.Status.Detail}");
        }
    }

}
