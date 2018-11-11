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
        private readonly IUrlHelper _urlHelper;


        public BooksController(ILibraryRepository repository,
            ILogger<BooksController> logger,
            IUrlHelper urlHelper)
        {
            _logger = logger;
            _repository = repository;
            _urlHelper = urlHelper;
        }

        [HttpGet(Name = "GetBooks")]
        public IActionResult GetBooks(Guid authorId)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var booksFromRepo = _repository.GetBooksForAuthor(authorId);
            var booksDto = Mapper.Map<IEnumerable<BookDto>>(booksFromRepo);

            booksDto = booksDto.Select(book =>
            {
                book = CreateLinksForBook(book);
                return book;
            });

            var booksWrapper = new LinkedCollectionResourceWrapperDto<BookDto>(booksDto);

            return Ok(CreateLinksForBooks(booksWrapper));
        }

        [HttpGet("{id}", Name = "GetBook")]
        public IActionResult GetBook(Guid authorId, Guid id)
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

            var bookDto = Mapper.Map<BookDto>(bookFromRepo);

            return Ok(CreateLinksForBook(bookDto));
        }

        [HttpPost(Name = "CreateBookForAuthor")]
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
                new { authorId = bookToReturn.AuthorId, id = bookToReturn.Id },
               CreateLinksForBook(bookToReturn));
        }

        [HttpDelete("{id}", Name = "DeleteBookForAuthor")]
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

        [HttpPut("{id}", Name = "UpdateBookForAuthor")]
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

                return CreatedAtRoute("GetBook", new { id = booktoReturn.Id }, booktoReturn);
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

        [HttpPatch("{id}", Name = "PartiallyUpdateBookForAuthor")]
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
                return CreatedAtRoute("GetBook", new { id = bookToReturn.Id }, bookToReturn);

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

        
        private BookDto CreateLinksForBook(BookDto book)
        {
            book.Links.Add(new LinkDto(
                _urlHelper.Link("GetBook",
                new { id = book.Id }),
                "self",
                "GET"));

            book.Links.Add(new LinkDto(
                _urlHelper.Link("DeleteBookForAuthor",
                new { id = book.Id }),
                "delete_book",
                "DELETE"));

            book.Links.Add(new LinkDto(
                _urlHelper.Link("UpdateBookForAuthor",
                new { id = book.Id }),
                "update_book",
                "UPDATE"));

            book.Links.Add(new LinkDto(
                _urlHelper.Link("PartiallyUpdateBookForAuthor",
                new { id = book.Id }),
                "partially_update_book",
                "PATCH"));

            return book;
        }

        private LinkedCollectionResourceWrapperDto<BookDto> CreateLinksForBooks(
            LinkedCollectionResourceWrapperDto<BookDto> booksWrapper)
        {
            booksWrapper.Links.Add(
                new LinkDto(_urlHelper.Link("GetBooks", new { }),
                    "self",
                    "GET"));

            return booksWrapper;
        }
    }
}
