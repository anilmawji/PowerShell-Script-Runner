﻿using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ITPortal.Lib.Services.Authentication.External;

public class ExternalAuthStateProvider : AuthenticationStateProvider
{
    private readonly IAuthenticationService _authenticationService;
    private AuthenticatedUser _currentUser;

    public ExternalAuthStateProvider(IAuthenticationService authenticationService)
    {
        _currentUser = new AuthenticatedUser();
        _authenticationService = authenticationService;
    }

    public void SetAuthenticationStateAsync(AuthenticatedUser user)
    {
        _currentUser = user;
        // Use BlazorWebView to update auth state
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(_currentUser.Principal));

    public async Task LogInAsync()
    {
        SetAuthenticationStateAsync(await LoginWithExternalProviderAsync());
    }

    private async Task<AuthenticatedUser> LoginWithExternalProviderAsync()
    {
        var authResult = await _authenticationService.AcquireTokenInteractiveAsync();

        if (authResult == null)
        {
            // Authentication failed, return a logged out user state
            return new AuthenticatedUser();
        }
        AuthenticatedUser user = new(authResult.ClaimsPrincipal);

        // For some reason AAD uses "name" as the claim type instead of ClaimTypes.Name
        // The user context only recognizes ClaimTypes.Name
        var claimTypeReplacements = new Dictionary<string, string>()
        {
            { "name", ClaimTypes.Name },
            { "preferred_username", ClaimTypes.Email }
        };
        user.ReplaceClaimTypes(claimTypeReplacements);

        return user;
    }

    public void Logout()
    {
        SetAuthenticationStateAsync(new AuthenticatedUser());
    }
}
