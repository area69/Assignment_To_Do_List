namespace Assignment_To_Do_List.Models
{

    public class UserInfo
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
    }
    public class TaskModel
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? AssignedTo { get; set; }
        public string? Details { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateOfCompletion { get; set; }
        public string? Status { get; set; }
    }
}
