using System;

[Serializable]
public class CreateRunRequest
{
    public string classId;
    public CreateRunRequest(string classId) { this.classId = classId; }
}
