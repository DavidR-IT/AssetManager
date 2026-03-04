using AssetManager.Data;
using AssetManager.Helpers;
using AssetManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AssetManager.Models.ViewModels;

namespace AssetManager.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Users - List all users with their roles
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();

            var assetCounts = await _context.Assets
                .Where(a => a.UserId != null)
                .GroupBy(a => a.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            var countLookup = assetCounts
                .Where(x => x.UserId.HasValue)
                .ToDictionary(x => x.UserId!.Value, x => x.Count);

            var userViewModels = users.Select(user => new UserViewModel
            {
                Id = user.Id.ToString(),
                UserName = user.Email,
                Email = user.Email,
                Roles = new List<string> { user.Role },
                AssetCount = countLookup.TryGetValue(user.Id, out var count) ? count : 0
            }).ToList();

            return View(userViewModels);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            ViewBag.Roles = new List<string> { Roles.Admin, Roles.Approval, Roles.Issuer, Roles.Viewer };
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate email
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    ViewBag.Roles = new List<string> { Roles.Admin, Roles.Approval, Roles.Issuer, Roles.Viewer };
                    return View(model);
                }

                // Check role selection
                var selectedRole = model.SelectedRoles?.FirstOrDefault();
                if (string.IsNullOrEmpty(selectedRole))
                {
                    ModelState.AddModelError(string.Empty, "Please select at least one role.");
                    ViewBag.Roles = new List<string> { Roles.Admin, Roles.Approval, Roles.Issuer, Roles.Viewer };
                    return View(model);
                }

                // Create user - no null checks needed, model validation handles it
                var user = new User
                {
                    Email = model.Email,  // Guaranteed non-null
                    PasswordHash = PasswordHelper.HashPassword(model.Password),  // Guaranteed non-null
                    Role = selectedRole,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = new List<string> { Roles.Admin, Roles.Approval, Roles.Issuer, Roles.Viewer };
            return View(model);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var assets = await _context.Assets
                .Where(a => a.UserId == user.Id || a.AssignedTo == user.Email)
                .ToListAsync();

            var model = new UserDetailViewModel
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                UserName = user.Email,
                Roles = new List<string> { user.Role },
                Assets = assets
            };

            return View(model);
        }

        // GET: Users/ViewUserAssets/5
        public async Task<IActionResult> ViewUserAssets(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var assets = await _context.Assets
                .Where(a => a.UserId == user.Id || a.AssignedTo == user.Email)
                .ToListAsync();

            ViewBag.UserName = user.Email;
            ViewBag.UserId = id;

            return View(assets);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                SelectedRole = user.Role,
                IsActive = user.IsActive
            };

            ViewBag.Roles = new List<string> { Roles.Admin, Roles.Approval, Roles.Issuer, Roles.Viewer };
            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditUserViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users.FindAsync(id);
                    if (user == null)
                        return NotFound();

                    // Store old email before changing
                    var oldEmail = user.Email;

                    // Check if email is being changed and if it already exists
                    if (user.Email != model.Email)
                    {
                        if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != id))
                        {
                            ModelState.AddModelError("Email", "Email already exists.");
                            ViewBag.Roles = new List<string> { Roles.Admin, Roles.Approval, Roles.Issuer, Roles.Viewer };
                            return View(model);
                        }

                        // Update all assets assigned to the old email
                        var assignedAssets = await _context.Assets
                            .Where(a => a.AssignedTo == oldEmail)
                            .ToListAsync();

                        foreach (var asset in assignedAssets)
                        {
                            asset.AssignedTo = model.Email;
                        }
                    }

                    // Update user properties - Email is now non-nullable
                    user.Email = model.Email;  // No null check needed
                    user.Role = model.SelectedRole;
                    user.IsActive = model.IsActive;

                    // Update password only if provided
                    if (!string.IsNullOrWhiteSpace(model.NewPassword))
                    {
                        // Password match validation is already handled by [Compare] attribute
                        user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await UserExists(id))
                        return NotFound();
                    throw;
                }
            }

            ViewBag.Roles = new List<string> { Roles.Admin, Roles.Approval, Roles.Issuer, Roles.Viewer };
            return View(model);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            // Check if user has assigned assets
            var assignedAssets = await _context.Assets
                .Where(a => a.UserId == user.Id || a.AssignedTo == user.Email)
                .CountAsync();

            // Check if user has pending requests
            var pendingRequests = await _context.AssetRequests
                .Where(r => r.UserId == user.Id && r.Status == RequestStatuses.Pending)
                .CountAsync();

            ViewBag.AssignedAssetsCount = assignedAssets;
            ViewBag.PendingRequestsCount = pendingRequests;

            var model = new UserViewModel
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                UserName = user.Email,
                Roles = new List<string> { user.Role },
                AssetCount = assignedAssets
            };

            return View(model);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            // Prevent deleting yourself
            var currentUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var cid) ? cid : 0;
            if (currentUserId == id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            // Handle all assets associated with this user (both by UserId and AssignedTo email)
            var assignedAssets = await _context.Assets
                .Where(a => a.UserId == user.Id || a.AssignedTo == user.Email)
                .ToListAsync();

            // Cancel any pending requests and revert their assets back to Ready
            var pendingRequests = await _context.AssetRequests
                .Include(r => r.RequestedAssets)
                    .ThenInclude(ri => ri.Asset)
                .Include(r => r.Asset)
                .Where(r => r.UserId == user.Id && r.Status == RequestStatuses.Pending)
                .ToListAsync();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (assignedAssets.Any())
                {
                    foreach (var asset in assignedAssets)
                    {
                        asset.AssignedTo = null;
                        asset.AssignedDate = null;
                        asset.UserId = null;
                        asset.Status = AssetStatus.Ready;
                    }
                }

                foreach (var request in pendingRequests)
                {
                    request.Status = RequestStatuses.Cancelled;
                    request.Notes = (request.Notes ?? "") + $" — Cancelled: user account deleted by admin.";

                    foreach (var item in request.RequestedAssets)
                    {
                        if (item.Asset != null && item.Asset.Status == AssetStatus.Pending)
                        {
                            item.Asset.Status = AssetStatus.Ready;
                            item.Asset.UserId = null;
                            item.Asset.AssignedTo = null;
                            item.Asset.AssignedDate = null;
                        }
                    }

                    if (request.Asset != null && request.Asset.Status == AssetStatus.Pending)
                    {
                        request.Asset.Status = AssetStatus.Ready;
                        request.Asset.UserId = null;
                        request.Asset.AssignedTo = null;
                        request.Asset.AssignedDate = null;
                    }
                }

                // Hard delete the user — SetNull behaviour on the FK automatically nullifies
                // UserId on all remaining request history, preserving the audit trail
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to delete user {UserId}", id);
                TempData["Error"] = "Failed to delete user. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            var cancelledCount = pendingRequests.Count;
            var successMsg = $"User {user.Email} has been deleted successfully. " +
                             $"{assignedAssets.Count} asset(s) returned to Ready status.";
            if (cancelledCount > 0)
                successMsg += $" {cancelledCount} pending request(s) were cancelled.";

            TempData["Success"] = successMsg;
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> UserExists(int id)
        {
            return await _context.Users.AnyAsync(e => e.Id == id);
        }
    }
}
