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
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly ITypeHelperService _typeHelperService;

        public AuthorsController(ILibraryRepository repository,
            IUrlHelper urlHelper,
            IPropertyMappingService propertyMappingService,
            ITypeHelperService typeHelperService)
        {
            _repository = repository;
            _urlhelper = urlHelper;
            _propertyMappingService = propertyMappingService;
            _typeHelperService = typeHelperService;
        }

        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters,
            [FromHeader(Name = "Accept")] string mediaType)
        {
            if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
            {
                return BadRequest();
            }

            if (!_typeHelperService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
            {
                return BadRequest();
            }

            var authorsFromRepo = _repository.GetAuthors(authorsResourceParameters);

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            if (mediaType == "application/vnd.arubesu.hateoas+json")
            {
                var paginationMetaData = new
                {
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages
                };

                Response.Headers.Add("X-Pagination",
                    Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetaData));

                var links = CreateLinksForAuthors(authorsResourceParameters,
                    authorsFromRepo.HasPrevious, authorsFromRepo.HasNext);

                var shapedAuthors = authors.ShapeData(authorsResourceParameters.Fields);

                var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
                {
                    var authorAsDictionary = author as IDictionary<string, object>;
                    var authorLinks = CreateLinksForAuthor(
                        (Guid)authorAsDictionary["Id"], authorsResourceParameters.Fields);

                    authorAsDictionary.Add("links", authorLinks);

                    return authorAsDictionary;
                });

                var linkedCollectionResource = new
                {
                    value = shapedAuthorsWithLinks,
                    links
                };

                return Ok(linkedCollectionResource);
            }
            else
            {
                var previousPageLink = authorsFromRepo.HasPrevious ?
                    CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage) :
                    null;

                var nextPageLink = authorsFromRepo.HasPrevious ?
                    CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage) :
                    null;

                var paginationMetadata = new
                {
                    previousPageLink,
                    nextPageLink,
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages
                };

                Response.Headers.Add("X-Pagination",
                    Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

                return Ok(authors.ShapeData(authorsResourceParameters.Fields));
            }

        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id, [FromQuery] string fields)
        {
            if (!_typeHelperService.TypeHasProperties<AuthorDto>
             (fields))
            {
                return BadRequest();
            }

            var authorFromRepo = _repository.GetAuthor(id);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            var author = Mapper.Map<AuthorDto>(authorFromRepo);

            var links = CreateLinksForAuthor(id, fields);

            var linkedResourceToReturn = author.ShapeData(fields)
                as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return Ok(linkedResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthor")]
        [RequestHeaderMatchesMediaType("Content-Type",
            new[] { "application/vnd.arubesu.author.full+json" })]
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

            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            var linkedResourceToReturn = authorToReturn.ShapeData(null)
                as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new { id = linkedResourceToReturn["Id"] }, linkedResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
        [RequestHeaderMatchesMediaType("Content-type",
            new [] { "application/vnd.arubesu.authorwithdateofdeath.full+json"})]
        public IActionResult CreateAuthorWithDateOfDeath([FromBody] AuthorWithDateOfDeathDto authorDto)
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

            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            var linkedResourceToReturn = authorToReturn.ShapeData(null)
                as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new { id = linkedResourceToReturn["Id"] }, linkedResourceToReturn);
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

        [HttpDelete("{id}", Name = "DeleteAuthor")]
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
                            fields = authorsResourceParameters.Fields,
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
                          fields = authorsResourceParameters.Fields,
                          orderBy = authorsResourceParameters.OrderBy,
                          searchQuery = authorsResourceParameters.SearchQuery,
                          genre = authorsResourceParameters.Genre,
                          pageNumber = authorsResourceParameters.PageNumber + 1,
                          pageSize = authorsResourceParameters.PageSize
                      });

                case ResourceUriType.Current:
                default:
                    return _urlhelper.Link("GetAuthors",
                      new
                      {
                          fields = authorsResourceParameters.Fields,
                          orderBy = authorsResourceParameters.OrderBy,
                          searchQuery = authorsResourceParameters.SearchQuery,
                          genre = authorsResourceParameters.Genre,
                          pageNumber = authorsResourceParameters.PageNumber,
                          pageSize = authorsResourceParameters.PageSize
                      });
            }
        }

        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id, string fields)
        {
            var links = new List<LinkDto>();

            if (string.IsNullOrEmpty(fields))
            {
                links.Add(new LinkDto(
                    _urlhelper.Link("GetAuthor", new { id }),
                    "self",
                    "GET"));
            }
            else
            {
                links.Add(new LinkDto(
                    _urlhelper.Link("GetAuthor", new { id, fields }),
                    "self",
                    "GET"));
            }

            links.Add(new LinkDto(
                   _urlhelper.Link("DeleteAuthor", new { id }),
                   "delete_author",
                   "DELETE"));

            links.Add(new LinkDto(
                   _urlhelper.Link("CreateBookForAuthor", new { authorId = id }),
                   "create_book_for_author",
                   "POST"));

            links.Add(new LinkDto(
                   _urlhelper.Link("GetBooks", new { authorId = id }),
                   "books",
                   "GET"));

            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForAuthors(
            AuthorsResourceParameters authorsResourceParameters,
            bool hasPrevious, bool hasNext)
        {
            var links = new List<LinkDto>();

            links.Add(
                new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters,
                    ResourceUriType.Current),
                    "self", "GET"));
            if (hasNext)
            {
                links.Add(
                new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters,
                    ResourceUriType.NextPage),
                    "nextPage", "GET"));
            }

            if (hasPrevious)
            {
                links.Add(
               new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters,
                   ResourceUriType.PreviousPage),
                   "previousPage", "GET"));
            }

            return links;
        }
    }
}
