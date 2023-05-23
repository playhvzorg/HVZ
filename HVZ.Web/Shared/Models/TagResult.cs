namespace HVZ.Web.Shared.Models
{
    public class TagResult
    {
        /// <summary>
        /// The success status of the operation
        /// </summary>
        public bool Succeeded { get; set; }
        /// <summary>
        /// Error message produced by the operation
        /// </summary>
        /// <remarks>Null if operation succeeded</remarks>
        public string? Error { get; set; }
        /// <summary>
        /// Updated tag count for the caller
        /// </summary>
        public int? Tags { get; set; }
        /// <summary>
        /// User ID of the tagged player
        /// </summary>
        public string? ReceiverUserId { get; set; }
        /// <summary>
        /// The number of required tags for an OZ
        /// </summary>
        /// <remarks>Null if caller is not an OZ</remarks>
        public int? OzTags { get; set; }
    }
}
