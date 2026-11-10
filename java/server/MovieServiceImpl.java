/*
   Data última atualização: 10/11/2026
   Estudante: Maria Eduarda Bambini
   Implementação do serviço gRPC MovieService.
*/

package server;

import java.util.List;

import com.google.protobuf.Empty;

import common.Style;
import io.grpc.Status;
import io.grpc.stub.StreamObserver;
import mflix.CreateMovieRequest;
import mflix.DeleteMovieRequest;
import mflix.GetMovieRequest;
import mflix.ListByActorRequest;
import mflix.ListByGenreRequest;
import mflix.Movie;
import mflix.MovieServiceGrpc;
import mflix.UpdateMovieRequest;

/* Serviço gRPC responsável pelas operações de filmes e pelo acesso ao repositório MongoDB. */
public class MovieServiceImpl extends MovieServiceGrpc.MovieServiceImplBase {

    private final MovieRepository repository;
    
    public MovieServiceImpl(MovieRepository repository) {
        this.repository = repository;
    }

    /* Valida o título e cadastra o filme no banco. */
    @Override
    public void createMovie(CreateMovieRequest request, StreamObserver<Movie> observer) {
        Style.showInfo("CreateMovie");
        try {
            Movie movie = request.getMovie();
            if (movie.getTitle().isEmpty()) {
                invalid(observer, "O título não pode ser vazio");
                return;
            }
            String newId = repository.insert(movie);
            observer.onNext(movie.toBuilder().setId(newId).build());
            observer.onCompleted();
        } catch (Exception e) {
            fail(observer, e);
        }
    }

    /* Busca um filme pelo seu id. */
    @Override
    public void getMovie(GetMovieRequest request, StreamObserver<Movie> observer) {
        Style.showInfo("GetMovie");
        try {
            if (request.getId().isEmpty()) {
                invalid(observer, "O id não pode ser vazio");
                return;
            }
            Movie movie = repository.findById(request.getId());
            if (movie == null) {
                notFound(observer);
                return;
            }
            observer.onNext(movie);
            observer.onCompleted();
        } catch (Exception e) {
            fail(observer, e);
        }
    }

    /* Atualiza os dados de um filme existente. */
    @Override
    public void updateMovie(UpdateMovieRequest request, StreamObserver<Movie> observer) {
        Style.showInfo("UpdateMovie");
        try {
            Movie movie = request.getMovie();
            if (movie.getId().isEmpty()) {
                invalid(observer, "O id não pode ser vazio");
                return;
            }
            if (!repository.update(movie)) {
                notFound(observer);
                return;
            }
            observer.onNext(repository.findById(movie.getId()));
            observer.onCompleted();
        } catch (Exception e) {
            fail(observer, e);
        }
    }

    /* Remove um filme pelo id. */
    @Override
    public void deleteMovie(DeleteMovieRequest request, StreamObserver<Empty> observer) {
        Style.showInfo("DeleteMovie");
        try {
            if (request.getId().isEmpty()) {
                invalid(observer, "O id não pode ser vazio");
                return;
            }
            if (!repository.delete(request.getId())) {
                notFound(observer);
                return;
            }
            observer.onNext(Empty.getDefaultInstance());
            observer.onCompleted();
        } catch (Exception e) {
            fail(observer, e);
        }
    }

    /* Lista os filmes que possuem o ator informado. */
    @Override
    public void listByActor(ListByActorRequest request, StreamObserver<Movie> observer) {
        Style.showInfo("ListByActor");
        try {
            if (request.getActor().isEmpty()) {
                invalid(observer, "O nome do ator não pode ser vazio");
                return;
            }
            stream(observer, repository.findByActor(request.getActor()));
        } catch (Exception e) {
            fail(observer, e);
        }
    }

    /* Lista os filmes de uma determinada categoria. */
    @Override
    public void listByGenre(ListByGenreRequest request, StreamObserver<Movie> observer) {
        Style.showInfo("ListByGenre");
        try {
            if (request.getGenre().isEmpty()) {
                invalid(observer, "A categoria não pode ser vazia");
                return;
            }
            stream(observer, repository.findByGenre(request.getGenre()));
        } catch (Exception e) {
            fail(observer, e);
        }
    }

    /* Envia os filmes encontrados pelo stream. */
    private void stream(StreamObserver<Movie> observer, List<Movie> movies) {
        for (Movie movie : movies) {
            observer.onNext(movie);
        }
        observer.onCompleted();
    }

    /* Retorna erro quando algum dado da requisição é inválido. */
    private void invalid(StreamObserver<?> observer, String message) {
        observer.onError(Status.INVALID_ARGUMENT.withDescription(message).asRuntimeException());
    }

    /* Retorna erro quando o filme não é encontrado. */
    private void notFound(StreamObserver<?> observer) {
        observer.onError(Status.NOT_FOUND.withDescription("Filme não encontrado").asRuntimeException());
    }

    /* Trata erros de validação e falhas internas do servidor. */
    private void fail(StreamObserver<?> observer, Exception e) {
        if (e instanceof IllegalArgumentException) {
            invalid(observer, "Id inválido");
            return;
        }
        e.printStackTrace();
        Style.showError(e.toString());
        observer.onError(Status.INTERNAL.withDescription("Erro interno no servidor").asRuntimeException());
    }
}