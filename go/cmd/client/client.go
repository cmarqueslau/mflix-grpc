/*
   Data de criação: 10/11/2026
   Estudante: Gustavo Martins França
   Definição do cliente
*/

package main

import (
	"bufio"
	"context"
	"fmt"
	"io"
	"log"
	"os"
	"strconv"
	"strings"
	"time"

	"mflix/internal/pb"

	"google.golang.org/grpc"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/credentials/insecure"
	"google.golang.org/grpc/status"
)

type Client struct {
	addr        string
	conn        *grpc.ClientConn
	rpc         pb.MovieServiceClient
	inputReader *bufio.Reader
}

// Client constructor
func NewClient(addr string) *Client {
	return &Client{
		addr: addr,
	}
}

// Connects a client to a server
func (c *Client) Connect() error {
	var err error

	// A new client needs gRPC credentials
	c.conn, err = grpc.NewClient(c.addr, grpc.WithTransportCredentials((insecure.NewCredentials())))

	if err != nil {
		return err
	}

	c.inputReader = bufio.NewReader(os.Stdin)
	c.rpc = pb.NewMovieServiceClient(c.conn)

	fmt.Printf("Connected on %s\n", c.addr)
	return nil
}

// Shows the menu and dispatches the chosen option until the user exits
func (c *Client) Run() {
	err := c.Connect()

	if err != nil {
		log.Fatal("Error connecting to server")
	}

	defer c.conn.Close()

	for {
		c.printMenu()

		var err error

		switch c.prompt("\n>>") {
		case "1":
			err = c.Create()

		case "2":
			err = c.Delete()

		case "3":
			err = c.Update()

		case "4":
			err = c.Get()

		case "5":
			err = c.ListByActor()

		case "6":
			err = c.ListByGenre()

		case "0":
			fmt.Printf("Disconnected from %s\n", c.addr)
			return

		default:
			fmt.Println("\nInvalid option\n")
		}

		if err != nil {
			log.Printf("Connection error: %s", err)
			return
		}
	}
}

// Prints every available option to the client
func (c *Client) printMenu() {
	fmt.Println("1. Adicionar novo filme")
	fmt.Println("2. Remover filme")
	fmt.Println("3. Alterar filme")
	fmt.Println("4. Buscar filme por ID")
	fmt.Println("5. Listar filmes por ator")
	fmt.Println("6. Listar filmes por categoria")
	fmt.Println("0. Sair")
}

// Handles option 1
func (c *Client) Create() error {
	m := &pb.Movie{
		Title:     c.prompt("Título"),
		Plot:      c.prompt("Sinopse"),
		Fullplot:  c.prompt("Sinopse completa"),
		Genres:    splitList(c.prompt("Gêneros (separados por vírgula)")),
		Cast:      splitList(c.prompt("Elenco (separados por vírgula)")),
		Directors: splitList(c.prompt("Diretores (separados por vírgula)")),
		Countries: splitList(c.prompt("Países (separados por vírgula)")),
		Year:      toInt32(c.prompt("Ano")),
		Runtime:   toInt32(c.prompt("Duração (min)")),
		Rated:     c.prompt("Classificação"),
		Type:      "movie",
	}

	ctx, cancel := remoteCallCtx()

	defer cancel()

	created, err := c.rpc.CreateMovie(ctx, &pb.CreateMovieRequest{Movie: m})

	if err != nil {
		return c.handleServerError(err)
	}

	fmt.Println("Filme criado:")
	printMovie(created)
	return nil
}

// Handles option 2
func (c *Client) Delete() error {
	id := c.prompt("ID")

	ctx, cancel := remoteCallCtx()
	defer cancel()

	_, err := c.rpc.DeleteMovie(ctx, &pb.DeleteMovieRequest{Id: id})

	if err != nil {
		return c.handleServerError(err)
	}

	fmt.Println("Filme removido")
	return nil
}

// Handles option 3
func (c *Client) Update() error {
	id := c.prompt("ID do filme")

	// Context for GetMovie() is separated from context for UpdateMovie() to avoid timeouts during prompts
	ctx, cancel := remoteCallCtx()
	m, err := c.rpc.GetMovie(ctx, &pb.GetMovieRequest{Id: id})
	cancel()

	if err != nil {
		return c.handleServerError(err)
	}

	fmt.Println("(Enter mantém o valor atual)")

	if s := c.prompt("Título [" + m.Title + "]"); s != "" {
		m.Title = s
	}

	if s := c.prompt("Sinopse [" + m.Plot + "]"); s != "" {
		m.Plot = s
	}

	if s := c.prompt("Gêneros [" + strings.Join(m.Genres, ",") + "]"); s != "" {
		m.Genres = splitList(s)
	}

	if s := c.prompt("Elenco [" + strings.Join(m.Cast, ",") + "]"); s != "" {
		m.Cast = splitList(s)
	}

	if s := c.prompt("Diretores [" + strings.Join(m.Directors, ",") + "]"); s != "" {
		m.Directors = splitList(s)
	}

	if s := c.prompt("Ano [" + strconv.Itoa(int(m.Year)) + "]"); s != "" {
		m.Year = toInt32(s)
	}

	ctx, cancel = remoteCallCtx()

	defer cancel()

	updated, err := c.rpc.UpdateMovie(ctx, &pb.UpdateMovieRequest{Movie: m})

	if err != nil {
		return c.handleServerError(err)
	}

	fmt.Println("Filme atualizado:")
	printMovie(updated)
	return nil
}

// Handles option 4
func (c *Client) Get() error {
	ctx, cancel := remoteCallCtx()
	defer cancel()

	m, err := c.rpc.GetMovie(ctx, &pb.GetMovieRequest{Id: c.prompt("ID")})

	if err != nil {
		return c.handleServerError(err)
	}

	printMovie(m)
	return nil
}

// Handles option 5
func (c *Client) ListByActor() error {
	ctx, cancel := remoteCallCtx()
	defer cancel()

	stream, err := c.rpc.ListByActor(ctx, &pb.ListByActorRequest{Actor: c.prompt("Nome do ator")})

	if err != nil {
		return c.handleServerError(err)
	}

	return c.printMovieStream(stream)
}

// Handles option 6
func (c *Client) ListByGenre() error {
	ctx, cancel := remoteCallCtx()
	defer cancel()

	stream, err := c.rpc.ListByGenre(ctx, &pb.ListByGenreRequest{Genre: c.prompt("Categoria")})

	if err != nil {
		return c.handleServerError(err)
	}

	return c.printMovieStream(stream)
}

// Handles errors sent by the server
func (c *Client) handleServerError(err error) error {
	st, _ := status.FromError(err)

	if st.Code() == codes.Unavailable {
		return err
	}

	fmt.Printf("\nError %s: %s\n\n", st.Code(), st.Message())
	return nil
}

// Prompts the user for an option and returns the trimmed line
func (c *Client) prompt(in string) string {
	fmt.Printf("%s: ", in)

	str, err := c.inputReader.ReadString('\n')

	if err != nil {
		log.Fatalf("Error reading input: %s", err)
	}

	return strings.TrimSpace(str)
}

// Reads every movie from a stream until the server finishes it
func (c *Client) printMovieStream(stream grpc.ServerStreamingClient[pb.Movie]) error {
	var movies []*pb.Movie

	for {
		m, err := stream.Recv()

		if err == io.EOF {
			break
		}

		if err != nil {
			return c.handleServerError(err)
		}

		movies = append(movies, m)
	}

	printMovies(movies)
	return nil
}

// Helper to use context with timeout
func remoteCallCtx() (context.Context, context.CancelFunc) {
	return context.WithTimeout(context.Background(), 10*time.Second)
}
