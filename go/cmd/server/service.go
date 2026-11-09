/*
   Data de criação: 08/11/2026
   Estudante: Gustavo Martins França
   Definição dos serviços RPC
*/

package main

import (
	"context"
	"errors"
	"log"

	"mflix/internal/pb"
	"mflix/internal/repository"

	"google.golang.org/grpc"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/peer"
	"google.golang.org/grpc/status"
	"google.golang.org/protobuf/types/known/emptypb"
)

type MovieService struct {
	pb.UnimplementedMovieServiceServer
	repo *repository.MovieRepository
}

// MovieService constructor
func NewMovieService(repo *repository.MovieRepository) *MovieService {
	return &MovieService{
		repo: repo,
	}
}

// Handles CreateMovie
func (s *MovieService) CreateMovie(ctx context.Context, req *pb.CreateMovieRequest) (*pb.Movie, error) {
	logRemoteCall(ctx, "CREATE")

	m := req.GetMovie()

	if m == nil || m.Title == "" {
		return nil, status.Error(codes.InvalidArgument, "Title is requierd")
	}

	created, err := s.repo.Create(ctx, m)

	if err != nil {
		return nil, toGrpcStatus(err)
	}

	return created, nil
}

// Handles GetMovie
func (s *MovieService) GetMovie(ctx context.Context, req *pb.GetMovieRequest) (*pb.Movie, error) {
	logRemoteCall(ctx, "GET")

	if req.GetId() == "" {
		return nil, status.Error(codes.InvalidArgument, "Id is empty")
	}

	movie, err := s.repo.Get(ctx, req.Id)

	if err != nil {
		return nil, toGrpcStatus(err)
	}

	return movie, nil
}

// Handles UpdateMovie
func (s *MovieService) UpdateMovie(ctx context.Context, req *pb.UpdateMovieRequest) (*pb.Movie, error) {
	logRemoteCall(ctx, "UPDATE")

	m := req.GetMovie()

	if m == nil || m.Id == "" {
		return nil, status.Error(codes.InvalidArgument, "Id is empty")
	}

	if m.Title == "" {
		return nil, status.Error(codes.InvalidArgument, "Title is empty")
	}

	updated, err := s.repo.Update(ctx, m)

	if err != nil {
		return nil, toGrpcStatus(err)
	}

	return updated, nil
}

// Handles DeleteMovie
func (s *MovieService) DeleteMovie(ctx context.Context, req *pb.DeleteMovieRequest) (*emptypb.Empty, error) {
	logRemoteCall(ctx, "DELETE")

	if req.GetId() == "" {
		return nil, status.Error(codes.InvalidArgument, "Id is empty")
	}

	if err := s.repo.Delete(ctx, req.Id); err != nil {
		return nil, toGrpcStatus(err)
	}

	return &emptypb.Empty{}, nil
}

// Handles ListByActor
func (s *MovieService) ListByActor(req *pb.ListByActorRequest, stream grpc.ServerStreamingServer[pb.Movie]) error {
	logRemoteCall(stream.Context(), "LIST_BY_ACTOR")

	if req.GetActor() == "" {
		return status.Error(codes.InvalidArgument, "Actor is empty")
	}

	movies, err := s.repo.ListByActor(stream.Context(), req.Actor)

	if err != nil {
		return toGrpcStatus(err)
	}

	return sendMovies(stream, movies)
}

// Handles ListByGente
func (s *MovieService) ListByGenre(req *pb.ListByGenreRequest, stream grpc.ServerStreamingServer[pb.Movie]) error {
	logRemoteCall(stream.Context(), "LIST_BY_GENRE")

	if req.GetGenre() == "" {
		return status.Error(codes.InvalidArgument, "Genre is empty")
	}

	movies, err := s.repo.ListByGenre(stream.Context(), req.Genre)

	if err != nil {
		return toGrpcStatus(err)
	}

	return sendMovies(stream, movies)
}

// Helper to send movies in a stream
func sendMovies(stream grpc.ServerStreamingServer[pb.Movie], movies []*pb.Movie) error {
	for _, m := range movies {
		if err := stream.Send(m); err != nil {
			return err
		}
	}

	return nil
}

// Converts a repository error into a gRPC status
func toGrpcStatus(err error) error {
	switch {
	case errors.Is(err, repository.ErrNotFound):
		return status.Error(codes.NotFound, err.Error())

	case errors.Is(err, repository.ErrInvalidID):
		return status.Error(codes.InvalidArgument, err.Error())

	default:
		log.Printf("Internal error: %s", err)
		return status.Error(codes.Internal, "internal error")
	}
}

// Logs every executed command
func logRemoteCall(ctx context.Context, command string) {
	addr := "unidentified"

	if p, ok := peer.FromContext(ctx); ok {
		addr = p.Addr.String()
	}

	log.Printf("client %s executed %s\n", addr, command)
}
