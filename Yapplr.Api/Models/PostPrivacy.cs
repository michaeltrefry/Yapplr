namespace Postr.Api.Models;

public enum PostPrivacy
{
    Public = 0,     // Anyone can see the post
    Followers = 1,  // Only followers can see the post
    Private = 2     // Only the author can see the post
}
