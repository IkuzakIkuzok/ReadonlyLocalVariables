
# ReadonlyLocalVariables

[![Test](https://github.com/IkuzakIkuzok/ReadonlyLocalVariables/actions/workflows/dotnet.yml/badge.svg)](https://github.com/IkuzakIkuzok/ReadonlyLocalVariables/actions/workflows/dotnet.yml)
[![Version](https://img.shields.io/nuget/v/ReadonlyLocalVariables?styles=flat)](https://www.nuget.org/packages/ReadonlyLocalVariables/#versions-body-tab)
[![Download](https://img.shields.io/nuget/dt/ReadonlyLocalVariables?styles=flat)](https://www.nuget.org/packages/ReadonlyLocalVariables/#versions-body-tab)
[![MIT License](http://img.shields.io/badge/license-MIT-blue.svg?style=flat)](https://github.com/IkuzakIkuzok/ReadonlyLocalVariables/blob/main/LICENSE)

[:us:](https://github.com/IkuzakIkuzok/ReadonlyLocalVariables/blob/main/README.md)

ローカル変数への再代入を禁止します。

## インストール方法

[NuGet](https://www.nuget.org/packages/ReadonlyLocalVariables/)からパッケージをダウンロードすることができます。

## 使用方法

アナライザをインストールすると，ローカル変数への再代入がエラーとなります。
`ReadonlyLocalVariables.ReassignableVariable`属性をメソッドに追加することで，例外的に再代入を許可するローカル変数を指定することができます。


```C#
using ReadonlyLocalVariables;
using System;

class C
{
    private static int Field = 0;

    [ReassignableVariable("reassignable")]  // Explicitly state which local variables are allowed to be reassigned.
    static void Main(string[] args)
    {
        var normal = 0;
        var reassignable = 0;

        Console.WriteLine(normal);
        Console.WriteLine(reassignable);
        Console.WriteLine(Field);

        normal = 1;        // Normal variables are treated as read-only.
        reassignable = 1;  // Specially marked local variables can be reassigned.
        Field = 1;         // Class members are not inspected because they have the `readonly` keyword.

        Console.WriteLine(normal);
        Console.WriteLine(reassignable);
        Console.WriteLine(Field);
    }
}
```

1つの属性で複数の識別子名を指定することもできます。


```C#
[ReassignableVariable("i", "j")]
void M()
{
    var i = 0;
    var j = 1;
}
```

### パラメータ

パラメータとして受け取った値もローカル変数と同様に再代入が禁止されます。
ただし，`out`パラメータ修飾子が付加されたパラメータについては，必ず値を書き換えなければならないためこの限りではありません。

### `out`引数

既に宣言されたローカル変数を`out`とともに引数として使用することも禁止されます。


```C#
var i = 0;
if (int.TryParse("1", out i))  // エラー
    Console.WriteLine(i);
```

このエラーを回避するには，`out var`による変数宣言を使用します。


```C#
if (int.TryParse("1", out var i))
    Console.WriteLine(i);
```

(推奨はされませんが属性による許可も利用できます。)

### タプル

既に宣言されたローカル変数を含むタプルへの代入もエラーとなります。

Assignments to tuples containing predefined local variables also result in an error.

```C#
var x = 0;
var y = 0;
(x, y) = (1, 2);  // エラー
```

代わりに，タプル内での変数宣言を利用してください。

```C#
(var x, var y) = (1, 2);
```

### for文

`for`文の制御部分でのローカル変数への再代入は検査されません。

```C#
for (var i = 0; i < 10; i += 2)  // OK
{
    i -= 1;  // エラー
}
```

### 複合代入

複合代入も代入操作であるため禁止されます。

## コード修正

コード修正機能により，再代入禁止エラーを2通りの方法で修正することができます。

ローカル変数への再代入を回避するために，新たな変数の宣言を追加することができます。

```diff
var local = 0;
Console.WriteLine(local);

-local = 1;
-Console.WriteLine(local);
+var local1 = 1;
+Console.WriteLine(local1);
```

新たな変数宣言以降の変数の参照は自動的に更新されます。
自動的に生成される識別子名は簡便なものであるため，適切な名前にリファクタリングすることを推奨します。

また，属性を追加することにより再代入を許可することもできます。


```diff
+using ReadonlyLocalVariables;

+[ReassignableVariable("local")]
void Func()
{
    var local = 0;
    Console.WriteLine(local);
    
    local = 1;
    Console.WriteLine(local);
}

タプルや`out`引数などについても同様に修正することができます。

# その他

## `CS8032`

アナライザを利用するプロジェクトをビルドする際に`CS8032`が発生する場合があります。
csprojを以下のように修正することで警告を解消できます。


```diff
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="ReadonlyLocalVariables" Version="2.5.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
+   <PackageReference Include="Microsoft.Net.Compilers.Toolset" Version="4.3.0">
+     <PrivateAssets>all</PrivateAssets>
+     <IncludeAssets>runtime; build; native; contentfiles; analyzers;</IncludeAssets>
+   </PackageReference>
  </ItemGroup>

</Project>
```
