namespace PropertyHub.Models.DTOs;
public class Result<T>
{
    public bool IsSuccess {get;set;}
    public T Data {get;set;}
    public string Error {get;set;}
    public static Result<T> Success(T Data)
    {
        return new Result<T> {IsSuccess = true, Data = Data};
    }
    public static Result<T> Failure(string Error)
    {
        return new Result<T> {IsSuccess = false, Error = Error};
    }
}