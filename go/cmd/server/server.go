/*
   Data de criação: 09/11/2026
   Estudante: Gustavo Martins França
   Definição do servidor
*/

package main

import (
	"log"
	"mflix/internal/pb"
	"mflix/internal/repository"
	"net"
	"os"

	"google.golang.org/grpc"
)

type Server struct {
	addr     string
	repo     *repository.MovieRepository
	grpc     *grpc.Server
	listener net.Listener
}

// Server Constructor
func NewServer(addr string, repo *repository.MovieRepository) *Server {
	return &Server{
		addr: addr,
		repo: repo,
	}
}

// Listens to addr and serves MovieService
func (s *Server) Run() {
	var err error
	s.listener, err = net.Listen("tcp", s.addr)

	if err != nil {
		log.Fatalf("Error listening to %s", s.addr)
	}

	defer s.listener.Close()

	if err := os.MkdirAll("logs/", os.ModePerm); err != nil {
		log.Fatalf("Could not create log directory: %s", err)
	}

	logFile, err := os.OpenFile("logs/server.log", os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0666)

	if err != nil {
		log.Fatalf("Could not open log file: %s", err)
	}

	defer logFile.Close()

	log.SetOutput(logFile)

	s.grpc = grpc.NewServer()
	pb.RegisterMovieServiceServer(s.grpc, NewMovieService(s.repo))

	if err := s.grpc.Serve(s.listener); err != nil {
		log.Fatalf("Error serving MovieService: %s", err)
	}
}
