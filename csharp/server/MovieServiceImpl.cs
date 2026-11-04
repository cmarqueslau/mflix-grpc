/*
   Data de criação: 03/11/2026
   Aluna: Caroline Marques Lau
   Implementação dos RPCs do serviço gRPC MovieService
*/

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;      // RpcException, Status, StatusCode, ServerCallContext, IServerStreamWriter
using Mflix;          // Movie, GetMovieRequest, MovieService... (gerados do .proto)
using MongoDB.Bson;   // ObjectId.TryParse
using Serilog;        // Log

public class MovieServiceImpl(MovieRepository repository) : MovieService.MovieServiceBase
{
    public override async Task<Movie> GetMovie(GetMovieRequest request, ServerCallContext context)
    {
        Log.Information("Comando recebido: consultar filme {Id}", request.Id);

        if (!ObjectId.TryParse(request.Id, out _))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ID do filme inválido."));

        Movie? movie = await repository.GetById(request.Id) ?? throw new RpcException(new Status(StatusCode.NotFound, "Filme não encontrado."));
        return movie;
    }

    public override async Task<Movie> CreateMovie(CreateMovieRequest request, ServerCallContext context)
    {
        Log.Information("Comando recebido: criar filme");

        if (request.Movie is null || string.IsNullOrWhiteSpace(request.Movie.Title))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "O filme precisa ter um título."));

        Movie createdMovie = await repository.Create(request.Movie);
        Log.Information("Filme criado com ID {Id}", createdMovie.Id);
        return createdMovie;
    }

    public override async Task<Empty> DeleteMovie(DeleteMovieRequest request, ServerCallContext context)
    {
        Log.Information("Comando recebido: deletar o filme com id {Id}", request.Id);

        if (!ObjectId.TryParse(request.Id, out _))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ID do filme inválido."));

        bool deleted = await repository.DeleteById(request.Id);
        if (!deleted)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Filme não encontrado"));
        }
        Log.Information("Filme {Id} excluído com sucesso.", request.Id);
        return new Empty();
    }

    public override async Task<Movie> UpdateMovie(UpdateMovieRequest request, ServerCallContext context)
    {
        if (request.Movie is null || string.IsNullOrWhiteSpace(request.Movie.Title))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "O filme precisa ter um título."));

        if (!ObjectId.TryParse(request.Movie.Id, out _))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ID do filme inválido."));

        Log.Information("Comando recebido: atualizar o filme de id {Id}", request.Movie.Id);
        Movie movie = await repository.UpdateById(request.Movie) ?? throw new RpcException(new Status(StatusCode.NotFound, "Filme não encontrado."));
        return movie;
    }

    public override async Task ListByActor(
        ListByActorRequest request,
        IServerStreamWriter<Movie> responseStream,
        ServerCallContext context
    )
    {
        if (string.IsNullOrWhiteSpace(request.Actor))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "O nome do ator não pode ser vazio."));
        Log.Information("Comando recebido: listar filmes do ator {Actor}", request.Actor);

        List<Movie> movies = await repository.ListByActor(request.Actor);
        foreach (Movie movie in movies)
        {
            await responseStream.WriteAsync(movie);
        }

        Log.Information("Enviados {Count} filmes do ator {Actor}", movies.Count, request.Actor);
    }

    public override async Task ListByGenre(
        ListByGenreRequest request,
        IServerStreamWriter<Movie> responseStream,
        ServerCallContext context
    )
    {
        if (string.IsNullOrWhiteSpace(request.Genre))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "O nome do gênero não pode ser vazio."));
        Log.Information("Comando recebido: listar filmes do ator {Actor}", request.Genre);
        List<Movie> movies = await repository.ListByGenre(request.Genre);
        foreach (Movie movie in movies)
        {
            await responseStream.WriteAsync(movie);
        }

        Log.Information("Enviados {Count} filmes do gênero {Actor}", movies.Count, request.Genre);
    }
}
