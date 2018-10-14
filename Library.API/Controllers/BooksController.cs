using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private readonly ILogger<BooksController> _logger;
        private readonly ILibraryRepository _repository;

        public BooksController(ILibraryRepository repository,
            ILogger<BooksController> logger)
        {
            _logger = logger;
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

            if (!ModelState.IsValid)
            {
                return new UnprocessabelEntityObjectResult(ModelState);
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

            _logger.LogInformation($"The book {id} for the author {authorId} was deleted.");
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
                var bookToCreate = Mapper.Map<Book>(bookDto);
                bookToCreate.Id = id;
                _repository.AddBookForAuthor(authorId, bookToCreate);

                if (!_repository.Save())
                {
                    throw new Exception($"Failed to Upserting the book {id}.");
                }

                var booktoReturn = Mapper.Map<BookDto>(bookToCreate);

                return CreatedAtRoute("GetBook", new { bookId = booktoReturn.Id }, booktoReturn);
                //return NotFound();
            }

            Mapper.Map(bookDto, bookFromRepo);

            if (!ModelState.IsValid)
            {
                return new UnprocessabelEntityObjectResult(ModelState);
            }

            _repository.UpdateBookForAuthor(bookFromRepo);

            if (!_repository.Save())
            {
                throw new Exception($"Failed on updating {id} book for the author {authorId}");
            }

            return NoContent();
        }

        [HttpPatch("{id}")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id,
            [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
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
                var bookDto = new BookForUpdateDto();
                patchDoc.ApplyTo(bookDto);

                var bookToAdd = Mapper.Map<Book>(bookDto);
                bookToAdd.Id = id;

                _repository.AddBookForAuthor(authorId, bookToAdd);

                if (!_repository.Save())
                {
                    throw new Exception($"Failed on upserting book {id} for author {id}.");
                }

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);
                return CreatedAtRoute("GetBook", new { bookId = bookToReturn.Id }, bookToReturn);

                //return NotFound();
            }

            var bookToPatch = Mapper.Map<BookForUpdateDto>(bookFromRepo);
            patchDoc.ApplyTo(bookToPatch, ModelState);


            TryValidateModel(bookToPatch);

            if (!ModelState.IsValid)
            {
                return new UnprocessabelEntityObjectResult(ModelState);
            }

            Mapper.Map(bookToPatch, bookFromRepo);

            _repository.UpdateBookForAuthor(bookFromRepo);

            if (!_repository.Save())
            {
                throw new Exception($"Failed on patching book {id} for author {authorId}");
            }

            return NoContent();

        }

    }
}
