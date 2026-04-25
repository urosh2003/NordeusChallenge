package com.example.backend.controllers;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("${api.base-path:/api}")
public class TestController {

    @GetMapping("/test")
    public String testApi() {
        return "API is working";
    }
}
