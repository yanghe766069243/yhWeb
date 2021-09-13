// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using System.Collections.Generic;
using System.Security.Claims;

namespace NewSchoolLive.IdentityService
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId()
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            { new ApiScope("Api1","testapi") };
        public static IEnumerable<ApiResource> ApiResources()
        {
            return new List<ApiResource>() { new ApiResource("Api12") { Scopes = { "Api1" } } };
        }
        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                new Client(){
                    ClientId="client",
                    ClientSecrets={ new Secret("1234".Sha256())},
                    AllowedGrantTypes=GrantTypes.ClientCredentials,AllowedScopes=new[]{ "Api1"}
                }
            };
    }
}