/*
    Última atualização: 10/11/2026
    Estudante: Maria Eduarda Bambini
    Acesso à coleção movies do MongoDB (banco sample_mflix).
*/

package server;

import com.mongodb.client.MongoClient;
import com.mongodb.client.MongoClients;
import com.mongodb.client.MongoCollection;
import com.mongodb.client.model.Filters;
import com.mongodb.client.model.Updates;
import com.mongodb.client.result.DeleteResult;
import com.mongodb.client.result.UpdateResult;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import mflix.Movie;
import org.bson.Document;
import org.bson.conversions.Bson;
import org.bson.types.ObjectId;

/* Classe responsável pelas operações de filmes no MongoDB. */
public class MovieRepository {

    private final MongoCollection<Document> collection;

    /* Conecta ao banco e seleciona a coleção de filmes. */
    public MovieRepository(String uri, String database, String collectionName) {
        MongoClient client = MongoClients.create(uri);
        collection = client.getDatabase(database).getCollection(collectionName);
    }

    /* Busca um filme pelo id. Retorna null se não encontrar. */
    public Movie findById(String id) {
        Document doc = collection.find(Filters.eq("_id", new ObjectId(id))).first();
        if (doc == null) {
            return null;
        }
        return toMovie(doc);
    }

    /* Insere um filme e retorna o id gerado pelo MongoDB. */
    public String insert(Movie movie) {
        Document doc = toDocument(movie);
        collection.insertOne(doc);
        return doc.getObjectId("_id").toHexString();
    }

    /* Atualiza os campos informados. Retorna false se o filme não existir. */
    public boolean update(Movie movie) {
        Bson filter = Filters.eq("_id", new ObjectId(movie.getId()));
        List<Bson> updates = new ArrayList<>();

        if (!movie.getTitle().isEmpty()) {
            updates.add(Updates.set("title", movie.getTitle()));
        }
        if (!movie.getPlot().isEmpty()) {
            updates.add(Updates.set("plot", movie.getPlot()));
        }
        if (!movie.getFullplot().isEmpty()) {
            updates.add(Updates.set("fullplot", movie.getFullplot()));
        }
        if (!movie.getRated().isEmpty()) {
            updates.add(Updates.set("rated", movie.getRated()));
        }
        if (!movie.getType().isEmpty()) {
            updates.add(Updates.set("type", movie.getType()));
        }
        if (!movie.getReleased().isEmpty()) {
            updates.add(Updates.set("released", movie.getReleased()));
        }
        if (movie.getRuntime() != 0) {
            updates.add(Updates.set("runtime", movie.getRuntime()));
        }
        if (movie.getYear() != 0) {
            updates.add(Updates.set("year", movie.getYear()));
        }
        if (movie.getGenresCount() > 0) {
            updates.add(Updates.set("genres", new ArrayList<String>(movie.getGenresList())));
        }
        if (movie.getCastCount() > 0) {
            updates.add(Updates.set("cast", new ArrayList<String>(movie.getCastList())));
        }
        if (movie.getDirectorsCount() > 0) {
            updates.add(Updates.set("directors", new ArrayList<String>(movie.getDirectorsList())));
        }
        if (movie.getCountriesCount() > 0) {
            updates.add(Updates.set("countries", new ArrayList<String>(movie.getCountriesList())));
        }

        if (updates.isEmpty()) {
            return collection.countDocuments(filter) > 0;
        }

        updates.add(Updates.set("lastupdated", LocalDateTime.now().toString()));
        UpdateResult result = collection.updateOne(filter, Updates.combine(updates));
        return result.getMatchedCount() > 0;
    }

    /* Exclui um filme pelo id. Retorna false se não encontrar. */
    public boolean delete(String id) {
        DeleteResult result = collection.deleteOne(Filters.eq("_id", new ObjectId(id)));
        return result.getDeletedCount() > 0;
    }

    /* Busca filmes pelo nome do ator. */
    public List<Movie> findByActor(String actor) {
        return findByField("cast", actor);
    }

    /* Busca filmes pelo gênero. */
    public List<Movie> findByGenre(String genre) {
        return findByField("genres", genre);
    }

    /* Busca filmes pelo campo informado. */
    private List<Movie> findByField(String field, String value) {
        List<Document> docs = collection.find(Filters.eq(field, value))
                .into(new ArrayList<Document>());

        List<Movie> movies = new ArrayList<>();
        for (int i = 0; i < docs.size(); i++) {
            movies.add(toMovie(docs.get(i)));
        }
        return movies;
    }

    /* Converte um documento do MongoDB para um objeto Movie. */
    private Movie toMovie(Document doc) {
        Movie.Builder builder = Movie.newBuilder();
        builder.setId(doc.getObjectId("_id").toHexString());
        builder.setTitle(getString(doc, "title"));
        builder.setPlot(getString(doc, "plot"));
        builder.setFullplot(getString(doc, "fullplot"));
        builder.setRated(getString(doc, "rated"));
        builder.setType(getString(doc, "type"));
        builder.setReleased(getString(doc, "released"));
        builder.setLastupdated(getString(doc, "lastupdated"));
        builder.setRuntime(getInt(doc, "runtime"));
        builder.setYear(getInt(doc, "year"));
        builder.setNumMflixComments(getInt(doc, "num_mflix_comments"));
        builder.addAllGenres(doc.getList("genres", String.class, new ArrayList<String>()));
        builder.addAllCast(doc.getList("cast", String.class, new ArrayList<String>()));
        builder.addAllDirectors(doc.getList("directors", String.class, new ArrayList<String>()));
        builder.addAllCountries(doc.getList("countries", String.class, new ArrayList<String>()));
        return builder.build();
    }

    /* Converte um objeto Movie em documento para salvar no MongoDB. */
    private Document toDocument(Movie movie) {
        Document doc = new Document();
        doc.append("title", movie.getTitle());
        doc.append("plot", movie.getPlot());
        doc.append("fullplot", movie.getFullplot());
        doc.append("rated", movie.getRated());
        doc.append("type", movie.getType());
        doc.append("released", movie.getReleased());
        doc.append("runtime", movie.getRuntime());
        doc.append("year", movie.getYear());
        doc.append("num_mflix_comments", 0);
        doc.append("lastupdated", LocalDateTime.now().toString());
        doc.append("genres", new ArrayList<String>(movie.getGenresList()));
        doc.append("cast", new ArrayList<String>(movie.getCastList()));
        doc.append("directors", new ArrayList<String>(movie.getDirectorsList()));
        doc.append("countries", new ArrayList<String>(movie.getCountriesList()));
        return doc;
    }

    /* Converte o valor do campo para texto. Retorna vazio se estiver ausente. */
    private String getString(Document doc, String field) {
        Object value = doc.get(field);
        if (value == null) {
            return "";
        }
        return value.toString();
    }

    /* Converte o campo para inteiro. Retorna 0 se não for numérico. */
    private int getInt(Document doc, String field) {
        Object value = doc.get(field);
        if (value instanceof Number) {
            return ((Number) value).intValue();
        }
        return 0;
    }
}