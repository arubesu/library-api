using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private readonly ILibraryRepository _repository;
        private readonly IUrlHelper _urlhelper;

        public AuthorsController(ILibraryRepository repository,
            IUrlHelper urlHelper)
        {
            _repository = repository;
            _urlhelper = urlHelper;
        }

        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters)
        {
            var authorsFromRepo = _repository.GetAuthors(authorsResourceParameters);

            var previousPageLink = authorsFromRepo.HasPrevious ?
                CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage) : null;

            var nextPageLink = authorsFromRepo.HasNext ?
               CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage) : null;

            var paginationMetaData = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                PreviousPageLink = previousPageLink,
                NextPageLink = nextPageLink
            };

            Response.Headers.Add("X-Pagination",
                Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetaData));

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);
            return Ok(authors);
        }

        private string CreateAuthorsResourceUri(
            AuthorsResourceParameters authorsResourceParameters,
            ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return _urlhelper.Link("GetAuthors",
                        new
                        {
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber - 1,
                            pageSize = authorsResourceParameters.PageSize
                        });
                case ResourceUriType.NextPage:
                    return _urlhelper.Link("GetAuthors",
                      new
                      {
                          orderBy = authorsResourceParameters.OrderBy,
                          searchQuery = authorsResourceParameters.SearchQuery,
                          genre = authorsResourceParameters.Genre,
                          pageNumber = authorsResourceParameters.PageNumber + 1,
                          pageSize = authorsResourceParameters.PageSize
                      });

                default:
                    return _urlhelper.Link("GetAuthors",
                      new
                      {
                          orderBy = authorsResourceParameters.OrderBy,
                          searchQuery = authorsResourceParameters.SearchQuery,
                          genre = authorsResourceParameters.Genre,
                          pageNumber = authorsResourceParameters.PageNumber,
                          pageSize = authorsResourceParameters.PageSize
                      });
            }
        }

        [HttpGet("{id}", Name = "GetAuthor")]
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
                throw new Exception("Failed on save this author.");
            }

            var authorToReturn = Mapper.Map<AuthorDto>(entity);

            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn);
        }

        [HttpPost("{id}")]
        public IActionResult BlockExistingAuthorPost(Guid id)
        {
            if (_repository.AuthorExists(id))
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            return NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAuthor(Guid id)
        {
            var authorFromRepo = _repository.GetAuthor(id);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            _repository.DeleteAuthor(authorFromRepo);

            if (!_repository.Save())
            {
                throw new Exception($"Failed on deleting Author {id}.");
            }

            return NoContent();
        }
    }
}
