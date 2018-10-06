using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorController : Controller
    {
        private readonly ILibraryRepository _repository;

        public AuthorController(ILibraryRepository repository)
        {
            _repository = repository;
        }

        [HttpGet()]
        public IActionResult GetAuthors()
        {
            var authorsFromRepo = _repository.GetAuthors();

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);
            return Ok(authors);
        }

        [HttpGet("{id}" , Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id)
        {
            var authorFromRepo = _repository.GetAuthor(id);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            var author = Mapper.Map<AuthorDto>(authorFromRepo);

            return Ok(author);
        }

       [HttpPost()]
       public IActionResult CreateAuthor([FromBody] AuthorForCreationDto authorDto)
        {
            if (authorDto == null)
            {
                return BadRequest();
            }

            var entity = Mapper.Map<Author>(authorDto);

            _repository.AddAuthor(entity);

            if (!_repository.Save())
            {
                return StatusCode(500);
            }

            var authorToReturn = Mapper.Map<AuthorDto>(entity);

            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn);
        }
    }
}
