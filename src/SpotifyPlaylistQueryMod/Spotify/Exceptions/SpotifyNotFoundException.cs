﻿using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyPlaylistQueryMod.Spotify.Exceptions.Base;

namespace SpotifyPlaylistQueryMod.Spotify.Exceptions;

public class SpotifyNotFoundException : SpotifyItemInaccessibleException
{
    public SpotifyNotFoundException(IResponse response) : base(response) { }
    public SpotifyNotFoundException(string message, Exception innerException) : base(message, innerException) { }

    public static bool CanCreate(APIException exception) =>
        exception.Response?.StatusCode == System.Net.HttpStatusCode.NotFound;
}