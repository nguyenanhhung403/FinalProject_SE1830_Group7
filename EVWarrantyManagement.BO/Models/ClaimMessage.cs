using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWarrantyManagement.BO.Models
{
    /// <summary>
    /// Represents a chat message related to a warranty claim
    /// Enables communication between service centers, technicians, and EVM staff
    /// </summary>
    public class ClaimMessage
    {
        /// <summary>
        /// Unique identifier for the message
        /// </summary>
        [Key]
        public int MessageId { get; set; }

        /// <summary>
        /// The claim this message belongs to
        /// </summary>
        [Required]
        public int ClaimId { get; set; }

        /// <summary>
        /// The user who sent the message
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// The message content
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// When the message was sent (UTC)
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the message has been read by all relevant parties
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// When the message was marked as read (UTC)
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// Navigation property to the related warranty claim
        /// </summary>
        [ForeignKey(nameof(ClaimId))]
        public virtual WarrantyClaim? WarrantyClaim { get; set; }

        /// <summary>
        /// Navigation property to the user who sent the message
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}
