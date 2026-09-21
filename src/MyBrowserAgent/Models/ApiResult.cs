namespace MyBrowserAgent.Models
{
    public sealed class ApiResult
    {
        public bool Success { get; set; }
        public object Data { get; set; }
        public string Error { get; set; }

        public static ApiResult Ok(object data = null) => new ApiResult { Success = true, Data = data };
        public static ApiResult Fail(string error) => new ApiResult { Success = false, Error = error };
    }
}
