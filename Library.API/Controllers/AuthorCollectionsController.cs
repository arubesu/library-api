using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.API.Controllers
{
    [Route("api/authorcollections")]
    public class AuthorCollectionsController : Controller
    {
        private readonly ILibraryRepository _repository;

        public AuthorCollectionsController(ILibraryRepository repository)
        {
            _repository = repository;
        }

        [HttpPost]
        public IActionResult CreateAuthors(
            [FromBody]IEnumerable<AuthorForCreationDto> authorDtos)
        {
            if (authorDtos == null)
            {
                return BadRequest();
            }

            var authorEntities = Mapper.Map<IEnumerable<Author>>(authorDtos);

            foreach (var author in authorEntities)
            {
                _repository.AddAuthor(author);
            }

            if (!_repository.Save())
            {
                throw new Exception("Failed to save authors");
            }

            //return Ok();
            var authorCollectionToReturn = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            var idAsString = String.Join(",",
                authorCollectionToReturn.Select(a => a.Id));

            return CreatedAtRoute("GetAuthorCollection", new { ids = idAsString }, authorCollectionToReturn);
        }

        [HttpGet("({ids})", Name = "GetAuthorCollection")]
        public IActionResult GetAuthorCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                return BadRequest();
            }

            var authorsEntities = _repository.GetAuthors(ids);

            if (ids.Count() != authorsEntities.Count())
            {
                return NotFound();
            }

            var authorsToReturn = Mapper.Map<IEnumerable<AuthorDto>>(authorsEntities);
            return Ok(authorsToReturn);
        }
    }
}