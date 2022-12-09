﻿export default {
    data() {
        return {
            streamId: ''
        }
    },
    emits: ['search'],
    props: {
        startingStreamId: String
    },
    methods: {
      submit() {
          this.$emit('search', this.streamId);
      }  
    },
    mounted() {
        this.streamId = this.startingStreamId;
        this.$refs.search.focus();
    },
    template: `
    <div class="card mb-3">
        <div class="card-body">
            <div class="mb-3">
                <form @submit="submit">
                    <label class="form-label" for="search">Stream ID</label>
                    <div class="input-group">
                        <input type="text" ref="search" id="search" class="form-control" placeholder="Enter stream ID" v-model="streamId">
                        <button class="btn btn-outline-secondary" type="submit">
                            <i class="bi-search"></i>
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>
    `
}