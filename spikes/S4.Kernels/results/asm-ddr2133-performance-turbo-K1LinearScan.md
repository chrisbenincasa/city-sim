## .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3 (Job: s4(Concurrent=True, Server=False))

```assembly
; S4.Kernels.Kernels.K1LinearScan.SpanUnchecked()
       push      rbp
       push      rbx
       push      rax
       lea       rbp,[rsp+10]
       mov       rax,[rdi+8]
       test      rax,rax
       je        near ptr M00_L06
       lea       rcx,[rax+10]
       mov       eax,[rax+8]
M00_L00:
       mov       rdx,[rdi+10]
       test      rdx,rdx
       je        near ptr M00_L07
       lea       rsi,[rdx+10]
       mov       edx,[rdx+8]
M00_L01:
       mov       rdi,[rdi+18]
       test      rdi,rdi
       je        near ptr M00_L08
       lea       r8,[rdi+10]
       mov       edi,[rdi+8]
M00_L02:
       xor       r9d,r9d
       test      eax,eax
       jle       short M00_L04
       cmp       eax,edx
       jg        short M00_L05
       cmp       eax,edi
       jg        short M00_L05
       xor       r9d,r9d
       mov       edx,eax
M00_L03:
       lea       r10,[rcx+r9*2]
       movsxd    rdi,dword ptr [rsi+r9]
       movsxd    r11,dword ptr [r8+r9]
       imul      rdi,r11
       sar       rdi,10
       movsxd    rdi,edi
       add       [r10],rdi
       add       r9,4
       dec       edx
       jne       short M00_L03
M00_L04:
       lea       edx,[rax-1]
       cmp       edx,eax
       jae       short M00_L09
       mov       eax,edx
       mov       rax,[rcx+rax*8]
       add       rsp,8
       pop       rbx
       pop       rbp
       ret
M00_L05:
       mov       r11d,r9d
       lea       r10,[rcx+r11*8]
       cmp       r9d,edx
       jae       short M00_L09
       movsxd    rbx,dword ptr [rsi+r11*4]
       cmp       r9d,edi
       jae       short M00_L09
       movsxd    r11,dword ptr [r8+r11*4]
       imul      r11,rbx
       sar       r11,10
       movsxd    r11,r11d
       add       [r10],r11
       inc       r9d
       cmp       r9d,eax
       jl        short M00_L05
       jmp       short M00_L04
M00_L06:
       xor       ecx,ecx
       xor       eax,eax
       jmp       near ptr M00_L00
M00_L07:
       xor       esi,esi
       xor       edx,edx
       jmp       near ptr M00_L01
M00_L08:
       xor       r8d,r8d
       xor       edi,edi
       jmp       near ptr M00_L02
M00_L09:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 225
```

## .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3 (Job: s4(Concurrent=True, Server=False))

```assembly
; S4.Kernels.Kernels.K1LinearScan.SpanChecked()
       push      rbp
       push      r15
       push      rbx
       lea       rbp,[rsp+10]
       mov       rax,[rdi+8]
       test      rax,rax
       je        near ptr M00_L06
       lea       rcx,[rax+10]
       mov       eax,[rax+8]
M00_L00:
       mov       rdx,[rdi+10]
       test      rdx,rdx
       je        near ptr M00_L07
       lea       rsi,[rdx+10]
       mov       edx,[rdx+8]
M00_L01:
       mov       rdi,[rdi+18]
       test      rdi,rdi
       je        near ptr M00_L08
       lea       r8,[rdi+10]
       mov       edi,[rdi+8]
M00_L02:
       xor       r9d,r9d
       test      eax,eax
       jle       short M00_L04
       cmp       eax,edx
       jg        short M00_L05
       cmp       eax,edi
       jg        short M00_L05
       xor       r9d,r9d
       mov       edx,eax
       nop       dword ptr [rax]
M00_L03:
       lea       r10,[rcx+r9*2]
       mov       rdi,[r10]
       movsxd    r11,dword ptr [rsi+r9]
       movsxd    rbx,dword ptr [r8+r9]
       imul      r11,rbx
       jo        near ptr M00_L10
       sar       r11,10
       movsxd    rbx,r11d
       cmp       r11,rbx
       jne       near ptr M00_L10
       movsxd    r11,r11d
       add       rdi,r11
       jo        near ptr M00_L10
       mov       [r10],rdi
       add       r9,4
       dec       edx
       jne       short M00_L03
M00_L04:
       lea       edx,[rax-1]
       cmp       edx,eax
       jae       short M00_L09
       mov       eax,edx
       mov       rax,[rcx+rax*8]
       pop       rbx
       pop       r15
       pop       rbp
       ret
M00_L05:
       mov       r11d,r9d
       lea       r10,[rcx+r11*8]
       mov       rbx,[r10]
       cmp       r9d,edx
       jae       short M00_L09
       movsxd    r15,dword ptr [rsi+r11*4]
       cmp       r9d,edi
       jae       short M00_L09
       movsxd    r11,dword ptr [r8+r11*4]
       imul      r11,r15
       jo        short M00_L10
       sar       r11,10
       movsxd    r15,r11d
       cmp       r11,r15
       jne       short M00_L10
       movsxd    r11,r11d
       add       r11,rbx
       jo        short M00_L10
       mov       [r10],r11
       inc       r9d
       cmp       r9d,eax
       jl        short M00_L05
       jmp       short M00_L04
M00_L06:
       xor       ecx,ecx
       xor       eax,eax
       jmp       near ptr M00_L00
M00_L07:
       xor       esi,esi
       xor       edx,edx
       jmp       near ptr M00_L01
M00_L08:
       xor       r8d,r8d
       xor       edi,edi
       jmp       near ptr M00_L02
M00_L09:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
M00_L10:
       call      CORINFO_HELP_OVERFLOW
       int       3
; Total bytes of code 285
```

## .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3 (Job: s4(Concurrent=True, Server=False))

```assembly
; S4.Kernels.Kernels.K1LinearScan.PointerUnchecked()
       push      rbp
       mov       rbp,rsp
       mov       rax,[rdi+20]
       mov       rcx,[rdi+28]
       mov       rdx,[rdi+30]
       xor       edi,edi
M00_L00:
       movsxd    rsi,edi
       lea       r8,[rax+rsi*8]
       movsxd    r9,dword ptr [rcx+rsi*4]
       movsxd    rsi,dword ptr [rdx+rsi*4]
       imul      rsi,r9
       sar       rsi,10
       movsxd    rsi,esi
       add       [r8],rsi
       inc       edi
       cmp       edi,0F4240
       jl        short M00_L00
       mov       rax,[rax+7A11F8]
       pop       rbp
       ret
; Total bytes of code 66
```

## .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3 (Job: s4(Concurrent=True, Server=False))

```assembly
; S4.Kernels.Kernels.K1LinearScan.PointerChecked()
       push      rbp
       mov       rbp,rsp
       mov       rax,[rdi+20]
       mov       rcx,[rdi+28]
       mov       rdx,[rdi+30]
       xor       edi,edi
M00_L00:
       movsxd    rsi,edi
       imul      r8,rsi,8
       jo        short M00_L01
       add       r8,rax
       mov       r9,[r8]
       imul      rsi,4
       jo        short M00_L01
       movsxd    r10,dword ptr [rcx+rsi]
       movsxd    rsi,dword ptr [rdx+rsi]
       imul      rsi,r10
       jo        short M00_L01
       sar       rsi,10
       movsxd    r10,esi
       cmp       rsi,r10
       jne       short M00_L01
       movsxd    rsi,esi
       add       rsi,r9
       jo        short M00_L01
       mov       [r8],rsi
       inc       edi
       cmp       edi,0F4240
       jl        short M00_L00
       mov       rax,[rax+7A11F8]
       pop       rbp
       ret
M00_L01:
       call      CORINFO_HELP_OVERFLOW
       int       3
; Total bytes of code 101
```

## .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3 (Job: s4(Concurrent=True, Server=False))

```assembly
; S4.Kernels.Kernels.K1LinearScan.PointerCheckedWalked()
       push      rbp
       mov       rbp,rsp
       mov       rax,[rdi+20]
       mov       rcx,[rdi+28]
       mov       rdx,[rdi+30]
       lea       rsi,[rax+7A1200]
       cmp       rax,rsi
       jae       short M00_L01
       nop       dword ptr [rax]
M00_L00:
       mov       r8,[rax]
       movsxd    r9,dword ptr [rcx]
       movsxd    r10,dword ptr [rdx]
       imul      r9,r10
       jo        short M00_L02
       sar       r9,10
       movsxd    r10,r9d
       cmp       r9,r10
       jne       short M00_L02
       movsxd    r9,r9d
       add       r8,r9
       jo        short M00_L02
       mov       [rax],r8
       add       rax,8
       add       rcx,4
       add       rdx,4
       cmp       rax,rsi
       jb        short M00_L00
M00_L01:
       mov       rax,[rdi+20]
       mov       rax,[rax+7A11F8]
       pop       rbp
       ret
M00_L02:
       call      CORINFO_HELP_OVERFLOW
       int       3
; Total bytes of code 106
```

