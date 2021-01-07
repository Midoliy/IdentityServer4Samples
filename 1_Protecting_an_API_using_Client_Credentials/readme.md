# クライアント資格情報を使用したAPIの保護

# 引用・参考元

- [IdentityServer4 公式サイト](https://identityserver4.readthedocs.io/en/latest/quickstarts/1_client_credentials.html)

# 事前準備

dotnet CLI の IdentityServer テンプレートをインストールします。
以下のコマンドでインストールが可能です。

```powershell
dotnet new -i IdentityServer4.Templates
```

# プロジェクトの作成

Identity Server／API／Clientの3つのプロジェクトを作成します。

```powershell
md src
cd src

# ソリューションファイルの作成
dotnet new sln -n IS4Sample

# Identity Serverプロジェクトの作成・追加
dotnet new is4empty -n IdentityServer
dotnet sln add ./IdentityServer

# APIプロジェクトの作成・追加
dotnet new webapi -n Api
dotnet sln add ./Api

# Clientプロジェクトの作成・追加
dotnet new console -n Client
dotnet sln add ./Client
```



## ターゲットフレームワークの更新

各プロジェクトのターゲットフレームワークを `.NET 5 以降` に設定します。
これをしないとなぜか動作しないです。

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <!-- ここを更新 -->
    <TargetFramework>net5.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="IdentityServer4" Version="4.0.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="3.2.0" />
  </ItemGroup>
</Project>

```



## パッケージの追加

各プロジェクトに必要なnugetパッケージを導入します。

```powershell
# APIプロジェクトにパッケージを追加
dotnet add ./Api package Microsoft.AspNetCore.Authentication.JwtBearer

# Clientプロジェクトにパッケージを追加
dotnet add ./Client package IdentityModel
```

パッケージの導入が完了したら、各パッケージを最新のものに更新しておきます。



## APIプロジェクトの不要ファイルを削除し、起動設定を修正する

デフォルトで作成される不要なファイルを削除しておきます。

- WeatherForecast.cs
- Controllers/WeatherForecastController.cs

また、以下のファイルを修正します。

- Api.csproj
- Properties/launchSettings.json

```xml
<!-- Api.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="5.0.1" NoWarn="NU1605" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="5.0.1" NoWarn="NU1605" />
    <!-- 以下の行を削除 -->
    <!-- <PackageReference Include="Swashbuckle.AspNetCore" Version="5.6.3" /> -->
  </ItemGroup>

</Project>
```

```json
// Properties/launchSettings.json
// 以下のように修正することでhttpsでのポートを6001番に固定できる
{
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:6000",
      "sslPort": 6001
    }
  },
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "SelfHost": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "applicationUrl": "https://localhost:6001"
    }
  }
}
```


# IdentityServerプロジェクトの編集

## Config.cs

APIスコープの定義とクライアントの定義を追加します。

```cs
using IdentityServer4.Models;
using System.Collections.Generic;

namespace IdentityServer
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
            { 
                // APIスコープの定義を追加
                new ApiScope("api1", "My API"),
            };

        public static IEnumerable<Client> Clients =>
            new Client[] 
            {
                // クライアントの定義を追加
                new Client
                {
                    ClientId = "client",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256()),
                    },
                    AllowedScopes = { "api1" }
                }
            };
    }
}
```


## Startup.cs

IdentityServerの構成を追加します。

```cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment environment)
        {
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var builder = services
                .AddIdentityServer()
                .AddInMemoryApiScopes(Config.ApiScopes)
                .AddInMemoryClients(Config.Clients);

            builder.AddDeveloperSigningCredential();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseIdentityServer();
        }
    }
}
```



# APIプロジェクトの編集

## IdentityController.cs

Contorllersフォルダ以下に `IdentityController.cs` を追加します。
中身は以下のようなシンプルなものにします。

```cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Api.Controllers
{
    [Route("identity")]
    [Authorize]
    public class IdentityController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return new JsonResult(from c in User.Claims select new { c.Type, c.Value });
        }
    }
}
```


## Startup.cs

トークンを検証し、APIを利用することが有効かどうか判定するための構成を追加します。

```cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Api
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // accepts any access token issued by identity server
            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = "https://localhost:5001";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false
                    };
                });

            // adds an authorization policy to make sure the token is for scope 'api1'
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "api1");
                });
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers()
                    .RequireAuthorization("ApiScope");
            });
        }
    }
}
```



# Clientプロジェクトの編集

## Program.cs

```cs
using IdentityModel.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // ---------------------------------------------
            // discover endpoints from metadata

            using var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var client = new HttpClient(httpClientHandler);
            var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5001");
            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return;
            }



            // ---------------------------------------------
            // request token

            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,

                ClientId = "client",
                ClientSecret = "secret",
                Scope = "api1"
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return;
            }

            Console.WriteLine(tokenResponse.Json);



            // ---------------------------------------------
            // call api

            // ここで取得したアクセストークンをセットする
            client.SetBearerToken(tokenResponse.AccessToken);

            var response = await client.GetAsync("https://localhost:6001/identity");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(JArray.Parse(content));
            }

            Console.ReadKey();
        }
    }
}
```