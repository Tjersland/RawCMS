﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RawCMS.Library.DataModel;
using RawCMS.Library.Service;
using RawCMS.Plugins.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace RawCMS.Plugins.Core.Extensions
{
    public static class JwtExstension
    {
        public static AuthenticationBuilder AddJwtProvider(this AuthenticationBuilder builder, ExternalProvider configuration, CRUDService cRUDService)
        {
            builder.AddJwtBearer(configuration.SchemaName, x =>
            {
                x.Authority = configuration.Authority;
                x.Audience = configuration.Audience;
                x.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    RoleClaimType = configuration.RoleClaimType
                };
                x.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async ctx =>
                    {
                        var accessToken = ctx.SecurityToken as JwtSecurityToken;
                        if (accessToken != null)
                        {
                            var client = new HttpClient();
                            var request = new HttpRequestMessage
                            {
                                Method = HttpMethod.Post,
                                RequestUri = new Uri(configuration.UserInfoEndpoint)
                            };
                            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken.RawData);
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                            var message = await client.SendAsync(request);
                            if (message.IsSuccessStatusCode)
                            {
                                var response = await message.Content.ReadAsStringAsync();
                                var userInfo = JsonConvert.DeserializeObject<JObject>(response);
                                if (ctx.Principal.Identity is ClaimsIdentity identity)
                                {
                                    foreach (var cl in userInfo.Properties())
                                    {
                                        if (identity.Claims.Where(y => y.Type == cl.Name).Count() == 0)
                                        {
                                            identity.AddClaim(new Claim(cl.Name, cl.Value.Value<string>()));
                                        }
                                    }


                                    var user = cRUDService.Query("_users", new DataQuery()
                                    {
                                        PageNumber = 1,
                                        PageSize = 1,
                                        RawQuery = @"{""Email"":""" + identity.Claims.FirstOrDefault(y => y.Type == "email")?.Value + @"""}"
                                    });

                                    if (user.TotalCount == 0)
                                    {
                                        var userToSave = new JObject
                                        {
                                            ["UserName"] = identity.Claims.FirstOrDefault(y => y.Type == "name")?.Value,
                                            ["Email"] = identity.Claims.FirstOrDefault(y => y.Type == "email")?.Value,
                                            ["IsExternal"] = true,
                                        };
                                        user.Items.Add(cRUDService.Insert("_users", userToSave));
                                    }
                                    

                                    string perm = ctx.Principal.FindFirstValue(configuration.RoleClaimType);
                                    var claimRole = identity.Claims.Where(y => y.Type == ClaimTypes.Role).FirstOrDefault() ?? new Claim(ClaimTypes.Role, string.Empty);
                                    var roles = string.Join(',', claimRole.Value, perm);

                                    if(user.Items.First["Roles"] != null)
                                    {
                                        roles = string.Join(',', roles, user.Items.First["Roles"].Values<string>()?.ToList());
                                    }
                                    identity.AddClaim(new Claim(ClaimTypes.Role, roles));
                                }
                            }
                        }
                    }
                };

                x.Validate();
            });
            return builder;
        }
    }
}
