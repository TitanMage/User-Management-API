using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UserManagementAPI.DTOs;
using UserManagementAPI.Models;
using UserManagementAPI.Repositories;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _repo;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserRepository repo, ILogger<UsersController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        /// <summary>
        /// Get all users with pagination
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<User>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                _logger.LogWarning("Invalid pagination parameters - pageNumber: {PageNumber}, pageSize: {PageSize}", pageNumber, pageSize);
                return BadRequest(new { message = "Page number and page size must be greater than 0." });
            }

            try
            {
                var users = await _repo.GetAllAsync(pageNumber, pageSize);
                var response = new PaginatedResponse<User>
                {
                    Data = users,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                throw;
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> Get(Guid id)
        {
            try
            {
                var user = await _repo.GetAsync(id);
                if (user == null)
                {
                    _logger.LogInformation("User not found - ID: {UserId}", id);
                    return NotFound(new { message = $"User with ID {id} not found." });
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<User>> Create([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid user creation request - ModelState errors");
                return BadRequest(ModelState);
            }

            try
            {
                // Check for duplicate email
                var existingUser = await _repo.GetByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Attempt to create user with duplicate email: {Email}", dto.Email);
                    return Conflict(new { message = "A user with this email already exists." });
                }

                var user = new User
                {
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    Email = dto.Email.Trim(),
                    DateOfBirth = dto.DateOfBirth
                };

                var createdUser = await _repo.CreateAsync(user);
                _logger.LogInformation("User created successfully - ID: {UserId}, Email: {Email}", createdUser.Id, createdUser.Email);
                return CreatedAtAction(nameof(Get), new { id = createdUser.Id }, createdUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] UserUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid user update request - ModelState errors");
                return BadRequest(ModelState);
            }

            try
            {
                var existing = await _repo.GetAsync(id);
                if (existing == null)
                {
                    _logger.LogInformation("User not found for update - ID: {UserId}", id);
                    return NotFound(new { message = $"User with ID {id} not found." });
                }

                // Check for duplicate email (excluding current user)
                var emailExists = await _repo.GetByEmailAsync(dto.Email);
                if (emailExists != null && emailExists.Id != id)
                {
                    _logger.LogWarning("Attempt to update user with duplicate email - ID: {UserId}, Email: {Email}", id, dto.Email);
                    return Conflict(new { message = "A user with this email already exists." });
                }

                existing.FirstName = dto.FirstName.Trim();
                existing.LastName = dto.LastName.Trim();
                existing.Email = dto.Email.Trim();
                existing.DateOfBirth = dto.DateOfBirth;

                await _repo.UpdateAsync(existing);
                _logger.LogInformation("User updated successfully - ID: {UserId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var deleted = await _repo.DeleteAsync(id);
                if (!deleted)
                {
                    _logger.LogInformation("User not found for deletion - ID: {UserId}", id);
                    return NotFound(new { message = $"User with ID {id} not found." });
                }
                _logger.LogInformation("User deleted successfully - ID: {UserId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID: {UserId}", id);
                throw;
            }
        }
    }
}