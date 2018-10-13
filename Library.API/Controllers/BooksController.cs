using AutoMapper;
using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private readonly ILibraryRepository _repository;

        public BooksController(ILibraryRepository repository)
        {
            _repository = repository;
        }

        [HttpGet()]
        public IActionResult GetBooks(Guid authorId)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var booksFromRepo = _repository.GetBooksForAuthor(authorId);
            var booksDto = Mapper.Map<IEnumerable<BookDto>>(booksFromRepo);

            return Ok(booksDto);
        }

        [HttpGet("{bookId}", Name = "GetBook")]
        public IActionResult GetBook(Guid authorId, Guid bookId)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookFromRepo = _repository.GetBookForAuthor(authorId, bookId);

            if (bookFromRepo == null)
            {
                return NotFound();
            }

            var bookDto = Mapper.Map<BookDto>(bookFromRepo);

            return Ok(bookDto);
        }

        [HttpPost()]
        public IActionResult CreateBookForAuthor(Guid authorId,
            [FromBody] BookForCreationDto bookDto)
        {
            if (bookDto == null)
            {
                return BadRequest();
            }

            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var entity = Mapper.Map<Book>(bookDto);

            _repository.AddBookForAuthor(authorId, entity);

            if (!_repository.Save())
            {
                throw new Exception("Failed on creating this book.");
            }

            var bookToReturn = Mapper.Map<BookDto>(entity);

            return CreatedAtRoute("GetBook",
                new { authorId = bookToReturn.AuthorId, bookId = bookToReturn.Id },
                bookToReturn);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookFromRepo = _repository.GetBookForAuthor(authorId, id);

            if (bookFromRepo == null)
            {
                return NotFound();
            }

            _repository.DeleteBook(bookFromRepo);

            if (!_repository.Save())
            {
                throw new Exception($"Failed on deleting book {id } from Author {authorId}");
            }

            return NoContent();
        }

        [HttpPut("{id}")]
        public IActionResult UpdateBookForAuthor(Guid id, Guid authorId,
            [FromBody] BookForUpdateDto bookDto)
        {
            if (bookDto == null)
            {
                return BadRequest();
            }

            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookFromRepo = _repository.GetBookForAuthor(authorId, id);

            if (bookFromRepo == null)
            {
                return NotFound();
            }

            Mapper.Map(bookDto, bookFromRepo);

            _repository.UpdateBookForAuthor(bookFromRepo);

            if (!_repository.Save())
            {
                throw new Exception($"Failed on updating {id} book for the author {authorId}");
            }

            return NoContent();
        }
    }
}
