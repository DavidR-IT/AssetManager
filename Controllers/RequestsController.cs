using AssetManager.Data;
using AssetManager.Helpers;
using AssetManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AssetManager.Controllers
{
    /// <summary>
    /// Handles asset request approvals and viewing
    /// Admin: View-only access to all requests (no approve/reject/cancel)
    /// Approval: Can approve/reject requests
    /// Issuer: Can view all requests for auditing purposes (read-only)
    /// </summary>
    [Authorize(Roles = "Admin,Approval,Issuer")]
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RequestsController> _logger;

        public RequestsController(ApplicationDbContext context, ILogger<RequestsController> logger)
        {
            _context = context;
            _logger = logger;
        }


        /// <summary>
        /// View pending requests
        /// Approval: Can approve/reject
        /// Admin/Issuer: Read-only for auditing
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var requests = await _context.AssetRequests
                .Include(r => r.Asset)
                .Include(r => r.RequestedAssets)
                    .ThenInclude(ri => ri.Asset)
                .Where(r => r.Status == RequestStatuses.Pending)
                .OrderBy(r => r.RequestDate)
                .AsNoTracking()
                .ToListAsync();

            return View(requests);
        }

        /// <summary>
        /// View all requests (complete history)
        /// Admin/Approval/Issuer: All can view for auditing
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AllRequests()
        {
            var requests = await _context.AssetRequests
                .Include(r => r.Asset)
                .Include(r => r.RequestedAssets)
                    .ThenInclude(ri => ri.Asset)
                .OrderByDescending(r => r.RequestDate)
                .AsNoTracking()
                .ToListAsync();

            return View(requests);
        }

        /// <summary>
        /// APPROVE REQUEST - GET - Show confirmation form with optional notes
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Approval")]
        public async Task<IActionResult> Approve(int id)
        {
            var req = await _context.AssetRequests
                .Include(r => r.Asset) // Legacy single asset
                .Include(r => r.RequestedAssets)
                    .ThenInclude(ri => ri.Asset) // New multi-asset support
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (req == null)
                return NotFound();

            // Check if using legacy single asset or new multi-asset
            if (req.Asset == null && !req.RequestedAssets.Any())
            {
                TempData["Error"] = "Request has no associated assets.";
                return RedirectToAction(nameof(Index));
            }

            if (req.Status != RequestStatuses.Pending)
            {
                TempData["Error"] = "Only pending requests can be approved.";
                return RedirectToAction(nameof(Index));
            }

            return View(req);
        }

        /// <summary>
        /// APPROVE REQUEST - POST - Confirm approval with optional notes
        /// </summary>
        [HttpPost, ActionName("Approve")]
        [Authorize(Roles = "Approval")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveConfirmed(int id, string? approvalNotes)
        {
            var req = await _context.AssetRequests
                .Include(r => r.Asset) // Legacy single asset
                .Include(r => r.RequestedAssets)
                    .ThenInclude(ri => ri.Asset) // New multi-asset support
                .FirstOrDefaultAsync(r => r.Id == id);

            if (req == null)
                return NotFound();

            if (req.Status != RequestStatuses.Pending)
            {
                TempData["Error"] = "Only pending requests can be approved.";
                return RedirectToAction(nameof(Index));
            }

            // Use transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                req.Status = RequestStatuses.Approved;
                req.ApprovedBy = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
                req.ApprovalDate = DateTime.UtcNow;

                // Add approval notes if provided
                if (!string.IsNullOrWhiteSpace(approvalNotes))
                {
                    req.Notes = req.Notes.AppendNote($"\n[Approval Notes by {req.ApprovedBy}]: {approvalNotes.Trim()}");
                }

                int assetCount = 0;

                // Handle new multi-asset requests
                if (req.RequestedAssets.Any())
                {
                    foreach (var requestItem in req.RequestedAssets)
                    {
                        if (requestItem.Asset != null)
                        {
                            if (requestItem.Asset.Status != AssetStatus.Pending)
                            {
                                await transaction.RollbackAsync();
                                TempData["Error"] = $"Asset {requestItem.Asset.AssetTag} is no longer in a Pending state.";
                                return RedirectToAction(nameof(Index));
                            }
                            requestItem.Asset.Status = AssetStatus.Assigned;
                            requestItem.Asset.AssignedTo = req.RequestedBy;
                            requestItem.Asset.AssignedDate = DateTime.UtcNow;
                            requestItem.Asset.UserId = req.UserId;
                            assetCount ++;
                        }
                    }
                }
                // Handle legacy single asset requests (backward compatibility)
                else if (req.Asset != null)
                {
                    if (req.Asset.Status != AssetStatus.Pending)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = $"Asset {req.Asset.AssetTag} is no longer in a Pending state and cannot be approved.";
                        return RedirectToAction(nameof(Index));
                    }
                    req.Asset.Status = AssetStatus.Assigned;
                    req.Asset.AssignedTo = req.RequestedBy;
                    req.Asset.AssignedDate = DateTime.UtcNow;
                    req.Asset.UserId = req.UserId;
                    assetCount = 1;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Request approved! {assetCount} asset(s) assigned to {req.RequestedBy}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to approve request {RequestId}", id);
                TempData["Error"] = "Failed to approve request. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Authorize(Roles = "Approval")]
        public async Task<IActionResult> Reject(int id)
        {
            var req = await _context.AssetRequests
                .Include(r => r.Asset) // Legacy single asset
                .Include(r => r.RequestedAssets)
                    .ThenInclude(ri => ri.Asset) // New multi-asset support
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (req == null)
                return NotFound();

            // Check if using legacy single asset or new multi-asset
            if (req.Asset == null && !req.RequestedAssets.Any())
            {
                TempData["Error"] = "Request has no associated assets.";
                return RedirectToAction(nameof(Index));
            }

            if (req.Status != RequestStatuses.Pending)
            {
                TempData["Error"] = "Only pending requests can be rejected.";
                return RedirectToAction(nameof(Index));
            }

            return View(req);
        }

        /// <summary>
        /// REJECT REQUEST - POST - Confirm rejection with optional notes
        /// </summary>
        [HttpPost, ActionName("Reject")]
        [Authorize(Roles = "Approval")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectConfirmed(int id, string? rejectionNotes)
        {
            var req = await _context.AssetRequests
                .Include(r => r.Asset) // Legacy single asset
                .Include(r => r.RequestedAssets)
                    .ThenInclude(ri => ri.Asset) // New multi-asset support
                .FirstOrDefaultAsync(r => r.Id == id);

            if (req == null)
                return NotFound();

            if (req.Status != RequestStatuses.Pending)
            {
                TempData["Error"] = "Only pending requests can be rejected.";
                return RedirectToAction(nameof(Index));
            }

            // Use transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                req.Status = RequestStatuses.Rejected;
                req.ApprovedBy = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
                req.ApprovalDate = DateTime.UtcNow;

                // Add rejection notes
                if (!string.IsNullOrWhiteSpace(rejectionNotes))
                {
                    req.Notes = req.Notes.AppendNote($"\n[Rejection Notes by {req.ApprovedBy}]: {rejectionNotes.Trim()}");
                }
                else
                {
                    req.Notes = req.Notes.AppendNote($"\n[Rejected by {req.ApprovedBy}]");
                }

                // Handle new multi-asset requests
                if (req.RequestedAssets.Any())
                {
                    foreach (var requestItem in req.RequestedAssets)
                    {
                        if (requestItem.Asset != null)
                        {
                            requestItem.Asset.Status = AssetStatus.Ready;
                            requestItem.Asset.AssignedTo = null;
                            requestItem.Asset.AssignedDate = null;
                            requestItem.Asset.UserId = null;
                        }
                    }
                }
                // Handle legacy single asset requests (backward compatibility)
                else if (req.Asset != null)
                {
                    req.Asset.Status = AssetStatus.Ready;
                    req.Asset.AssignedTo = null;
                    req.Asset.AssignedDate = null;
                    req.Asset.UserId = null;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Request rejected. Asset(s) returned to available status.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to reject request {RequestId}", id);
                TempData["Error"] = "Failed to reject request. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Cancel a pending request - Available to request owner and Approval role
        /// Allows users to cancel their own pending requests
        /// Approval role can also cancel any pending request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var req = await _context.AssetRequests
               .Include(r => r.Asset) // Legacy single asset
               .Include(r => r.RequestedAssets)
                   .ThenInclude(ri => ri.Asset) // New multi-asset support
               .FirstOrDefaultAsync(r => r.Id == id);

            if (req == null)
                return NotFound();

            // Check if request is pending
            if (req.Status != RequestStatuses.Pending)
            {
                TempData["Error"] = "Only pending requests can be cancelled.";
                return RedirectToAction(nameof(Index));
            }

            // Get current user info
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : 0;
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            // Check authorization: user must own the request OR be Approval role
            if ((!req.UserId.HasValue || req.UserId.Value != userId) && !User.IsInRole(Roles.Approval))
            {
                TempData["Error"] = "You can only cancel your own requests.";
                return RedirectToAction(nameof(Index));
            }

            // Use transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Cancel the request
                req.Status = RequestStatuses.Cancelled;
                req.ApprovedBy = userEmail ?? "User";
                req.ApprovalDate = DateTime.UtcNow;
                req.Notes = req.Notes.AppendNote($" - Cancelled by {userEmail}");

                int assetCount = 0;

                // Handle new multi-asset requests
                if (req.RequestedAssets.Any())
                {
                    foreach (var requestItem in req.RequestedAssets)
                    {
                        if (requestItem.Asset != null && requestItem.Asset.Status == AssetStatus.Pending)
                        {
                            requestItem.Asset.Status = AssetStatus.Ready;
                            assetCount++;
                        }
                    }
                }
                // Handle legacy single asset requests (backward compatibility)
                else if (req.Asset != null)
                {
                    // Only set asset to Ready if it's still in Pending status
                    // Don't change status if already Approved/Assigned
                    if (req.Asset.Status == AssetStatus.Pending)
                    {
                        req.Asset.Status = AssetStatus.Ready;
                    }
                    assetCount = 1;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Request cancelled successfully. {assetCount} asset(s) returned to available status.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to cancel request {RequestId}", id);
                TempData["Error"] = "Failed to cancel request. Please try again.";
            }

            // Redirect based on who cancelled
            if (User.IsInRole(Roles.Issuer))
            {
                return RedirectToAction("MyRequests", "Assets");
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// View request details
        /// All authorized roles can view
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.AssetRequests
                .Include(r => r.Asset)
                .Include(r => r.RequestedAssets)
                    .ThenInclude(ri => ri.Asset)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            return View(request);
        }
    }
 }