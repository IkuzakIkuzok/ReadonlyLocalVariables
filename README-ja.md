
# ReadonlyLocalVariables

[![Test](https://github.com/IkuzakIkuzok/ReadonlyLocalVariables/actions/workflows/dotnet.yml/badge.svg)](https://github.com/IkuzakIkuzok/ReadonlyLocalVariables/actions/workflows/dotnet.yml)
[![Version](https://img.shields.io/nuget/v/ReadonlyLocalVariables?styles=flat)](https://www.nuget.org/packages/ReadonlyLocalVariables/#versions-body-tab)
[![Download](https://img.shields.io/nuget/dt/ReadonlyLocalVariables?styles=flat)](https://www.nuget.org/packages/ReadonlyLocalVariables/#versions-body-tab)
[![MIT License](http://img.shields.io/badge/license-MIT-blue.svg?style=flat)](https://github.com/IkuzakIkuzok/ReadonlyLocalVariables/blob/main/LICENSE)

[:us:](https://github.com/IkuzakIkuzok/ReadonlyLocalVariables/blob/main/README.md)

���[�J���ϐ��ւ̍đ�����֎~���܂��B

## �C���X�g�[�����@

[NuGet](https://www.nuget.org/packages/ReadonlyLocalVariables/)����p�b�P�[�W���_�E�����[�h���邱�Ƃ��ł��܂��B

## �g�p���@

�A�i���C�U���C���X�g�[������ƁC���[�J���ϐ��ւ̍đ�����G���[�ƂȂ�܂��B
`ReadonlyLocalVariables.ReassignableVariable`���������\�b�h�ɒǉ����邱�ƂŁC��O�I�ɍđ���������郍�[�J���ϐ����w�肷�邱�Ƃ��ł��܂��B


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

1�̑����ŕ����̎��ʎq�����w�肷�邱�Ƃ��ł��܂��B


```C#
[ReassignableVariable("i", "j")]
void M()
{
    var i = 0;
    var j = 1;
}
```

### �p�����[�^

�p�����[�^�Ƃ��Ď󂯎�����l�����[�J���ϐ��Ɠ��l�ɍđ�����֎~����܂��B
�������C`out`�p�����[�^�C���q���t�����ꂽ�p�����[�^�ɂ��ẮC�K���l�����������Ȃ���΂Ȃ�Ȃ����߂��̌���ł͂���܂���B

### `out`����

���ɐ錾���ꂽ���[�J���ϐ���`out`�ƂƂ��Ɉ����Ƃ��Ďg�p���邱�Ƃ��֎~����܂��B


```C#
var i = 0;
if (int.TryParse("1", out i))  // �G���[
    Console.WriteLine(i);
```

���̃G���[���������ɂ́C`out var`�ɂ��ϐ��錾���g�p���܂��B


```C#
if (int.TryParse("1", out var i))
    Console.WriteLine(i);
```

(�����͂���܂��񂪑����ɂ�鋖�����p�ł��܂��B)

### �^�v��

���ɐ錾���ꂽ���[�J���ϐ����܂ރ^�v���ւ̑�����G���[�ƂȂ�܂��B

Assignments to tuples containing predefined local variables also result in an error.

```C#
var x = 0;
var y = 0;
(x, y) = (1, 2);  // �G���[
```

����ɁC�^�v�����ł̕ϐ��錾�𗘗p���Ă��������B

```C#
(var x, var y) = (1, 2);
```

### for��

`for`���̐��䕔���ł̃��[�J���ϐ��ւ̍đ���͌�������܂���B

```C#
for (var i = 0; i < 10; i += 2)  // OK
{
    i -= 1;  // �G���[
}
```

### �������

����������������ł��邽�ߋ֎~����܂��B

## �R�[�h�C��

�R�[�h�C���@�\�ɂ��C�đ���֎~�G���[��2�ʂ�̕��@�ŏC�����邱�Ƃ��ł��܂��B

���[�J���ϐ��ւ̍đ����������邽�߂ɁC�V���ȕϐ��̐錾��ǉ����邱�Ƃ��ł��܂��B

```diff
var local = 0;
Console.WriteLine(local);

-local = 1;
-Console.WriteLine(local);
+var local1 = 1;
+Console.WriteLine(local1);
```

�V���ȕϐ��錾�ȍ~�̕ϐ��̎Q�Ƃ͎����I�ɍX�V����܂��B
�����I�ɐ�������鎯�ʎq���͊ȕւȂ��̂ł��邽�߁C�K�؂Ȗ��O�Ƀ��t�@�N�^�����O���邱�Ƃ𐄏����܂��B

�܂��C������ǉ����邱�Ƃɂ��đ���������邱�Ƃ��ł��܂��B


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

�^�v����`out`�����Ȃǂɂ��Ă����l�ɏC�����邱�Ƃ��ł��܂��B

# ���̑�

## `CS8032`

�A�i���C�U�𗘗p����v���W�F�N�g���r���h����ۂ�`CS8032`����������ꍇ������܂��B
csproj���ȉ��̂悤�ɏC�����邱�ƂŌx���������ł��܂��B


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
