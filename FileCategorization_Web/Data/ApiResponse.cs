namespace FileCategorization_Web.Data
{
    /// <summary>
    /// Represents a generic API response containing data and a message.
    /// </summary>
    /// <typeparam name="T">The type of the data included in the response.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Gets or sets the data included in the API response.
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Gets or sets the message included in the API response.
        /// </summary>
        public string Message { get; set; }

    }
}